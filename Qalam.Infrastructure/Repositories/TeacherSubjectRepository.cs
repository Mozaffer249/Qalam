using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherSubjectRepository : GenericRepositoryAsync<TeacherSubject>, ITeacherSubjectRepository
{
    private readonly ApplicationDBContext _context;
    private readonly DbSet<TeacherSubject> _teacherSubjects;
    private readonly DbSet<TeacherSubjectUnit> _teacherSubjectUnits;

    public TeacherSubjectRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
        _teacherSubjects = context.Set<TeacherSubject>();
        _teacherSubjectUnits = context.Set<TeacherSubjectUnit>();
    }

    private static IQueryable<TeacherSubject> IncludeSubjectGraph(IQueryable<TeacherSubject> query) =>
        query
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Curriculum)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Level)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Grade)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.ParentSubject)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.QuranContentTypes)
                .ThenInclude(q => q.QuranContentType)
            .Include(ts => ts.QuranLevels)
            .Include(ts => ts.EducationLevels)
                .ThenInclude(el => el.EducationLevel)
            .Include(ts => ts.WritableFilters)
                .ThenInclude(wf => wf.WritableFilterValue);

    public async Task<List<TeacherSubject>> GetTeacherSubjectsWithUnitsAsync(int teacherId)
    {
        return await IncludeSubjectGraph(_teacherSubjects.AsNoTracking())
            .Where(ts => ts.TeacherId == teacherId)
            .OrderBy(ts => ts.Subject.NameAr)
            .ToListAsync();
    }

    public async Task<TeacherSubject?> GetTeacherSubjectWithUnitsAsync(int teacherSubjectId)
    {
        return await IncludeSubjectGraph(_teacherSubjects.AsNoTracking())
            .Where(ts => ts.Id == teacherSubjectId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TeacherSubject>> SaveTeacherSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjectDtos)
    {
        await RemoveAllTeacherSubjectsAsync(teacherId);

        foreach (var subjectDto in subjectDtos)
        {
            var teacherSubject = BuildTeacherSubject(teacherId, subjectDto);
            await _teacherSubjects.AddAsync(teacherSubject);
        }

        await _context.SaveChangesAsync();
        return await GetTeacherSubjectsWithUnitsAsync(teacherId);
    }

    public async Task<bool> TeacherHasSubjectAsync(int teacherId, int subjectId)
    {
        return await _teacherSubjects
            .AnyAsync(ts => ts.TeacherId == teacherId
                            && ts.SubjectId == subjectId
                            && ts.IsActive);
    }

    public async Task<bool> HasAnySubjectsAsync(int teacherId)
    {
        return await _teacherSubjects
            .AnyAsync(ts => ts.TeacherId == teacherId && ts.IsActive);
    }

    public async Task<bool> HasAnySubjectOfferingsAsync(int teacherId)
    {
        return await _teacherSubjects.AnyAsync(ts => ts.TeacherId == teacherId);
    }

    public async Task<TeacherSubjectActivationSnapshot> GetSubjectActivationSnapshotAsync(int teacherId)
    {
        var counts = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .GroupBy(_ => 1)
            .Select(g => new TeacherSubjectActivationSnapshot
            {
                Total = g.Count(),
                Active = g.Count(ts => ts.IsActive),
                Inactive = g.Count(ts => !ts.IsActive)
            })
            .FirstOrDefaultAsync();

        return counts ?? new TeacherSubjectActivationSnapshot();
    }

    public async Task RemoveAllTeacherSubjectsAsync(int teacherId)
    {
        var existingSubjects = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Include(ts => ts.TeacherSubjectUnits)
            .Include(ts => ts.QuranContentTypes)
            .Include(ts => ts.QuranLevels)
            .Include(ts => ts.WritableFilters)
            .ToListAsync();

        foreach (var subject in existingSubjects)
        {
            _teacherSubjectUnits.RemoveRange(subject.TeacherSubjectUnits);
            _context.TeacherSubjectQuranContentTypes.RemoveRange(subject.QuranContentTypes);
            _context.TeacherSubjectQuranLevels.RemoveRange(subject.QuranLevels);
            _context.TeacherSubjectWritableFilters.RemoveRange(subject.WritableFilters);
        }

        _teacherSubjects.RemoveRange(existingSubjects);
        await _context.SaveChangesAsync();
    }

    public async Task<HashSet<int>> GetExistingSubjectIdsAsync(int teacherId)
    {
        var subjectIds = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId && ts.IsActive)
            .Select(ts => ts.SubjectId)
            .ToListAsync();

        return subjectIds.ToHashSet();
    }

    public async Task<List<TeacherSubject>> AddNewSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjectDtos)
    {
        if (subjectDtos.Count == 0)
            return new List<TeacherSubject>();

        var existingSubjects = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Include(ts => ts.TeacherSubjectUnits)
            .Include(ts => ts.QuranContentTypes)
            .Include(ts => ts.QuranLevels)
            .Include(ts => ts.WritableFilters)
            .ToListAsync();

        var existingBySubjectId = existingSubjects.ToDictionary(ts => ts.SubjectId);
        var savedIds = new List<int>();

        foreach (var dto in subjectDtos)
        {
            if (existingBySubjectId.TryGetValue(dto.SubjectId, out var existing))
            {
                ApplySubjectDto(existing, dto);
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                savedIds.Add(existing.Id);
            }
            else
            {
                var teacherSubject = BuildTeacherSubject(teacherId, dto);
                await _teacherSubjects.AddAsync(teacherSubject);
                // Id assigned after SaveChanges; track via subjectId later
                existingBySubjectId[dto.SubjectId] = teacherSubject;
            }
        }

        await _context.SaveChangesAsync();

        var subjectIds = subjectDtos.Select(d => d.SubjectId).Distinct().ToList();
        return await IncludeSubjectGraph(_teacherSubjects.AsNoTracking())
            .Where(ts => ts.TeacherId == teacherId && subjectIds.Contains(ts.SubjectId))
            .ToListAsync();
    }

    public async Task<List<int>> GetActiveTeacherIdsBySubjectAsync(
        int subjectId,
        IReadOnlyCollection<int>? requiredQuranContentTypeIds = null,
        IReadOnlyCollection<int>? requiredQuranLevelIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _teacherSubjects
            .AsNoTracking()
            .Where(ts => ts.SubjectId == subjectId
                         && ts.IsActive
                         && ts.Teacher != null
                         && ts.Teacher.Status == TeacherStatus.Active);

        if (requiredQuranContentTypeIds is { Count: > 0 })
        {
            foreach (var typeId in requiredQuranContentTypeIds.Distinct())
            {
                var captured = typeId;
                query = query.Where(ts =>
                    !ts.QuranContentTypes.Any()
                    || ts.QuranContentTypes.Any(c => c.QuranContentTypeId == captured));
            }
        }

        if (requiredQuranLevelIds is { Count: > 0 })
        {
            foreach (var levelId in requiredQuranLevelIds.Distinct())
            {
                var captured = levelId;
                query = query.Where(ts =>
                    !ts.QuranLevels.Any()
                    || ts.QuranLevels.Any(l => l.QuranLevelId == captured));
            }
        }

        return await query
            .Select(ts => ts.TeacherId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TeacherSubject>> GetAllByTeacherIdForAdminAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        return await IncludeSubjectGraph(_teacherSubjects.AsNoTracking())
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .Where(ts => ts.TeacherId == teacherId)
            .OrderBy(ts => ts.Subject.NameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeacherSubject?> GetByIdForTeacherAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        return await IncludeSubjectGraph(_teacherSubjects)
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .Where(ts => ts.Id == teacherSubjectId && ts.TeacherId == teacherId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TeacherSubject?> GetBySubjectIdForTeacherAsync(
        int teacherId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return await IncludeSubjectGraph(_teacherSubjects)
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .Where(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DeleteOwnedAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        var teacherSubject = await _teacherSubjects
            .Where(ts => ts.Id == teacherSubjectId && ts.TeacherId == teacherId)
            .Include(ts => ts.TeacherSubjectUnits)
            .Include(ts => ts.QuranContentTypes)
            .Include(ts => ts.QuranLevels)
            .Include(ts => ts.WritableFilters)
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherSubject == null)
            return false;

        _teacherSubjectUnits.RemoveRange(teacherSubject.TeacherSubjectUnits);
        _context.TeacherSubjectQuranContentTypes.RemoveRange(teacherSubject.QuranContentTypes);
            _context.TeacherSubjectQuranLevels.RemoveRange(teacherSubject.QuranLevels);
            _context.TeacherSubjectEducationLevels.RemoveRange(teacherSubject.EducationLevels);
            _context.TeacherSubjectWritableFilters.RemoveRange(teacherSubject.WritableFilters);
        _teacherSubjects.Remove(teacherSubject);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TeacherSubject?> ReplaceUnitsAsync(
        int teacherId,
        int teacherSubjectId,
        bool canTeachFullSubject,
        List<TeacherSubjectUnitItemDto> units,
        IReadOnlyList<int>? quranContentTypeIds = null,
        IReadOnlyList<int>? quranLevelIds = null,
        IReadOnlyList<int>? educationLevelIds = null,
        CancellationToken cancellationToken = default)
    {
        var teacherSubject = await _teacherSubjects
            .Where(ts => ts.Id == teacherSubjectId && ts.TeacherId == teacherId)
            .Include(ts => ts.TeacherSubjectUnits)
            .Include(ts => ts.QuranContentTypes)
            .Include(ts => ts.QuranLevels)
            .Include(ts => ts.EducationLevels)
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherSubject == null)
            return null;

        teacherSubject.CanTeachFullSubject = canTeachFullSubject;
        teacherSubject.UpdatedAt = DateTime.UtcNow;

        _teacherSubjectUnits.RemoveRange(teacherSubject.TeacherSubjectUnits);
        teacherSubject.TeacherSubjectUnits.Clear();

        if (!canTeachFullSubject && units.Count > 0)
        {
            foreach (var unitId in units.Select(u => u.UnitId).Distinct())
            {
                teacherSubject.TeacherSubjectUnits.Add(new TeacherSubjectUnit
                {
                    TeacherSubjectId = teacherSubject.Id,
                    UnitId = unitId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        ReplaceQuranCoverage(teacherSubject, quranContentTypeIds ?? Array.Empty<int>(), quranLevelIds ?? Array.Empty<int>());
        ReplaceEducationLevels(teacherSubject, educationLevelIds ?? Array.Empty<int>());

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdForTeacherAsync(teacherId, teacherSubjectId, cancellationToken);
    }

    public async Task<PaginatedResult<TeacherSubject>> GetPagedForAdminAsync(
        int pageNumber,
        int pageSize,
        int? teacherId = null,
        int? subjectId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = IncludeSubjectGraph(_teacherSubjects.AsNoTracking())
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .AsQueryable();

        if (teacherId.HasValue)
            query = query.Where(ts => ts.TeacherId == teacherId.Value);
        if (subjectId.HasValue)
            query = query.Where(ts => ts.SubjectId == subjectId.Value);
        if (isActive.HasValue)
            query = query.Where(ts => ts.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(ts => ts.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TeacherSubject>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<List<int>> GetDistinctDomainIdsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default) =>
        await _teacherSubjects
            .AsNoTracking()
            .Where(ts => ts.TeacherId == teacherId && ts.Subject != null)
            .Select(ts => ts.Subject!.DomainId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public Task<List<TeacherSubject>> GetTeacherSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _teacherSubjects
            .Include(ts => ts.Subject)
            .Where(ts => ts.TeacherId == teacherId && ts.Subject!.DomainId == domainId)
            .ToListAsync(cancellationToken);

    public Task<List<TeacherSubject>> GetActiveSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _teacherSubjects
            .Include(ts => ts.Subject)
            .Where(ts => ts.TeacherId == teacherId
                         && ts.Subject!.DomainId == domainId
                         && ts.IsActive)
            .ToListAsync(cancellationToken);

    public Task<List<TeacherSubject>> GetInactiveSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _teacherSubjects
            .Include(ts => ts.Subject)
            .Where(ts => ts.TeacherId == teacherId
                         && ts.Subject!.DomainId == domainId
                         && !ts.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<List<TeacherSubject>> GetActiveSubjectsWithUnitsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        return await IncludeSubjectGraph(_teacherSubjects.AsNoTracking())
            .Where(ts => ts.TeacherId == teacherId && ts.IsActive)
            .OrderBy(ts => ts.Subject.NameAr)
            .ToListAsync(cancellationToken);
    }

    private static TeacherSubject BuildTeacherSubject(int teacherId, TeacherSubjectItemDto dto)
    {
        var teacherSubject = new TeacherSubject
        {
            TeacherId = teacherId,
            SubjectId = dto.SubjectId,
            CanTeachFullSubject = dto.CanTeachFullSubject,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (!dto.CanTeachFullSubject && dto.Units.Count > 0)
        {
            foreach (var unitId in dto.Units.Select(u => u.UnitId).Distinct())
            {
                teacherSubject.TeacherSubjectUnits.Add(new TeacherSubjectUnit
                {
                    UnitId = unitId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        ApplyQuranCoverage(teacherSubject, dto.QuranContentTypeIds, dto.QuranLevelIds);
        ApplyEducationLevels(teacherSubject, dto.EducationLevelIds);
        ApplyWritableFilters(teacherSubject, dto.WritableFilterValueIds);
        return teacherSubject;
    }

    private void ApplySubjectDto(TeacherSubject existing, TeacherSubjectItemDto dto)
    {
        existing.CanTeachFullSubject = dto.CanTeachFullSubject;

        _teacherSubjectUnits.RemoveRange(existing.TeacherSubjectUnits);
        existing.TeacherSubjectUnits.Clear();

        if (!dto.CanTeachFullSubject && dto.Units.Count > 0)
        {
            foreach (var unitId in dto.Units.Select(u => u.UnitId).Distinct())
            {
                existing.TeacherSubjectUnits.Add(new TeacherSubjectUnit
                {
                    TeacherSubjectId = existing.Id,
                    UnitId = unitId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        ReplaceQuranCoverage(existing, dto.QuranContentTypeIds, dto.QuranLevelIds);
        ReplaceEducationLevels(existing, dto.EducationLevelIds);
        ReplaceWritableFilters(existing, dto.WritableFilterValueIds);
    }

    private static void ApplyQuranCoverage(
        TeacherSubject teacherSubject,
        IEnumerable<int>? contentTypeIds,
        IEnumerable<int>? levelIds)
    {
        foreach (var typeId in (contentTypeIds ?? Array.Empty<int>()).Distinct())
        {
            teacherSubject.QuranContentTypes.Add(new TeacherSubjectQuranContentType
            {
                QuranContentTypeId = typeId,
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var levelId in (levelIds ?? Array.Empty<int>()).Distinct())
        {
            teacherSubject.QuranLevels.Add(new TeacherSubjectQuranLevel
            {
                QuranLevelId = levelId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private void ReplaceQuranCoverage(
        TeacherSubject teacherSubject,
        IReadOnlyList<int> contentTypeIds,
        IReadOnlyList<int> levelIds)
    {
        _context.TeacherSubjectQuranContentTypes.RemoveRange(teacherSubject.QuranContentTypes);
        teacherSubject.QuranContentTypes.Clear();
        _context.TeacherSubjectQuranLevels.RemoveRange(teacherSubject.QuranLevels);
        teacherSubject.QuranLevels.Clear();

        ApplyQuranCoverage(teacherSubject, contentTypeIds, levelIds);
        foreach (var row in teacherSubject.QuranContentTypes)
            row.TeacherSubjectId = teacherSubject.Id;
        foreach (var row in teacherSubject.QuranLevels)
            row.TeacherSubjectId = teacherSubject.Id;
    }

    private static void ApplyEducationLevels(
        TeacherSubject teacherSubject,
        IEnumerable<int>? educationLevelIds)
    {
        foreach (var levelId in (educationLevelIds ?? Array.Empty<int>()).Distinct())
        {
            teacherSubject.EducationLevels.Add(new TeacherSubjectEducationLevel
            {
                EducationLevelId = levelId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private void ReplaceEducationLevels(
        TeacherSubject teacherSubject,
        IReadOnlyList<int> educationLevelIds)
    {
        _context.TeacherSubjectEducationLevels.RemoveRange(teacherSubject.EducationLevels);
        teacherSubject.EducationLevels.Clear();
        ApplyEducationLevels(teacherSubject, educationLevelIds);
        foreach (var row in teacherSubject.EducationLevels)
            row.TeacherSubjectId = teacherSubject.Id;
    }

    private static void ApplyWritableFilters(
        TeacherSubject teacherSubject,
        IEnumerable<int>? writableFilterValueIds)
    {
        foreach (var valueId in (writableFilterValueIds ?? Array.Empty<int>()).Distinct())
        {
            teacherSubject.WritableFilters.Add(new TeacherSubjectWritableFilter
            {
                WritableFilterValueId = valueId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private void ReplaceWritableFilters(
        TeacherSubject teacherSubject,
        IReadOnlyList<int>? writableFilterValueIds)
    {
        _context.TeacherSubjectWritableFilters.RemoveRange(teacherSubject.WritableFilters);
        teacherSubject.WritableFilters.Clear();

        ApplyWritableFilters(teacherSubject, writableFilterValueIds);
        foreach (var row in teacherSubject.WritableFilters)
            row.TeacherSubjectId = teacherSubject.Id;
    }
}
