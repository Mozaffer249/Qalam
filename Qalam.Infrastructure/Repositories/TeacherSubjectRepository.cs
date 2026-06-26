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

    public async Task<List<TeacherSubject>> GetTeacherSubjectsWithUnitsAsync(int teacherId)
    {
        return await _teacherSubjects
            .AsNoTracking()
            .Where(ts => ts.TeacherId == teacherId)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranContentType)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranLevel)
            .OrderBy(ts => ts.Subject.NameAr)
            .ToListAsync();
    }

    public async Task<TeacherSubject?> GetTeacherSubjectWithUnitsAsync(int teacherSubjectId)
    {
        return await _teacherSubjects
            .AsNoTracking()
            .Where(ts => ts.Id == teacherSubjectId)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranContentType)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranLevel)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TeacherSubject>> SaveTeacherSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjectDtos)
    {
        // Remove existing subjects and their units
        await RemoveAllTeacherSubjectsAsync(teacherId);

        var newSubjects = new List<TeacherSubject>();

        foreach (var subjectDto in subjectDtos)
        {
            var teacherSubject = new TeacherSubject
            {
                TeacherId = teacherId,
                SubjectId = subjectDto.SubjectId,
                CanTeachFullSubject = subjectDto.CanTeachFullSubject,
                IsActive = true,
                VerificationStatus = DocumentVerificationStatus.Pending,
                RejectionReason = null,
                ReviewedByAdminId = null,
                ReviewedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await _teacherSubjects.AddAsync(teacherSubject);
            await _context.SaveChangesAsync(); // Save to get the ID

            // Add units if not teaching full subject
            if (!subjectDto.CanTeachFullSubject && subjectDto.Units.Any())
            {
                var units = subjectDto.Units.Select(u => new TeacherSubjectUnit
                {
                    TeacherSubjectId = teacherSubject.Id,
                    UnitId = u.UnitId,
                    QuranContentTypeId = u.QuranContentTypeId,
                    QuranLevelId = u.QuranLevelId,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _teacherSubjectUnits.AddRangeAsync(units);
            }

            newSubjects.Add(teacherSubject);
        }

        await _context.SaveChangesAsync();

        // Return with full data
        return await GetTeacherSubjectsWithUnitsAsync(teacherId);
    }

    public async Task<bool> TeacherHasSubjectAsync(int teacherId, int subjectId)
    {
        return await _teacherSubjects
            .AnyAsync(ts => ts.TeacherId == teacherId
                            && ts.SubjectId == subjectId
                            && ts.IsActive
                            && ts.VerificationStatus == DocumentVerificationStatus.Approved);
    }

    public async Task<bool> HasAnySubjectsAsync(int teacherId)
    {
        return await _teacherSubjects
            .AnyAsync(ts => ts.TeacherId == teacherId
                            && ts.IsActive
                            && ts.VerificationStatus == DocumentVerificationStatus.Approved);
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
                Pending = g.Count(ts => ts.VerificationStatus == DocumentVerificationStatus.Pending),
                Approved = g.Count(ts => ts.VerificationStatus == DocumentVerificationStatus.Approved),
                Rejected = g.Count(ts => ts.VerificationStatus == DocumentVerificationStatus.Rejected)
            })
            .FirstOrDefaultAsync();

        return counts ?? new TeacherSubjectActivationSnapshot();
    }

    public async Task RemoveAllTeacherSubjectsAsync(int teacherId)
    {
        var existingSubjects = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Include(ts => ts.TeacherSubjectUnits)
            .ToListAsync();

        foreach (var subject in existingSubjects)
        {
            // Units are cascade deleted, but let's be explicit
            _teacherSubjectUnits.RemoveRange(subject.TeacherSubjectUnits);
        }

        _teacherSubjects.RemoveRange(existingSubjects);
        await _context.SaveChangesAsync();
    }

    public async Task<HashSet<int>> GetExistingSubjectIdsAsync(int teacherId)
    {
        var subjectIds = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId
                         && ts.IsActive
                         && ts.VerificationStatus == DocumentVerificationStatus.Approved)
            .Select(ts => ts.SubjectId)  // Only select SubjectId - optimized
            .ToListAsync();

        return subjectIds.ToHashSet();
    }

    public async Task<List<TeacherSubject>> AddNewSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjectDtos)
    {
        // Load existing subjects WITH units
        var existingSubjects = await _teacherSubjects
            .Where(ts => ts.TeacherId == teacherId && ts.IsActive)
            .Include(ts => ts.TeacherSubjectUnits)
            .ToListAsync();

        // Generate signatures for existing subjects
        var existingSignatures = existingSubjects
            .Select(ts => GetTeacherSubjectSignature(ts))
            .ToHashSet();

        // Filter to truly new offerings
        var newSubjects = subjectDtos
            .Where(dto => !existingSignatures.Contains(GetSignatureFromDto(dto)))
            .ToList();

        // If nothing new, return empty list
        if (!newSubjects.Any())
        {
            return new List<TeacherSubject>();
        }

        // Track newly added subjects
        var addedSubjects = new List<TeacherSubject>();

        // Add new teaching offerings
        foreach (var dto in newSubjects)
        {
            var teacherSubject = new TeacherSubject
            {
                TeacherId = teacherId,
                SubjectId = dto.SubjectId,
                CanTeachFullSubject = dto.CanTeachFullSubject,
                IsActive = true,
                VerificationStatus = DocumentVerificationStatus.Approved,
                RejectionReason = null,
                RejectionSource = null,
                CreatedAt = DateTime.UtcNow
            };

            // Add units directly to the TeacherSubject collection
            // EF Core will automatically set the foreign key when saved
            if (!dto.CanTeachFullSubject && dto.Units.Any())
            {
                foreach (var unitDto in dto.Units)
                {
                    teacherSubject.TeacherSubjectUnits.Add(new TeacherSubjectUnit
                    {
                        UnitId = unitDto.UnitId,
                        QuranContentTypeId = unitDto.QuranContentTypeId,
                        QuranLevelId = unitDto.QuranLevelId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _teacherSubjects.AddAsync(teacherSubject);
            addedSubjects.Add(teacherSubject);
        }

        // Save everything in a single transaction
        await _context.SaveChangesAsync();

        // Reload added subjects with full navigation properties
        var addedIds = addedSubjects.Select(s => s.Id).ToList();
        return await _teacherSubjects
            .AsNoTracking()
            .Where(ts => addedIds.Contains(ts.Id))
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranContentType)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranLevel)
            .ToListAsync();
    }

    private string GetTeacherSubjectSignature(TeacherSubject ts)
    {
        var parts = new List<string> { ts.SubjectId.ToString() };

        parts.Add(ts.CanTeachFullSubject ? "FULL" : "PARTIAL");

        if (ts.TeacherSubjectUnits?.Any() == true)
        {
            var unitSigs = ts.TeacherSubjectUnits
                .OrderBy(u => u.UnitId)
                .ThenBy(u => u.QuranContentTypeId ?? 0)
                .ThenBy(u => u.QuranLevelId ?? 0)
                .Select(u => $"{u.UnitId}:{u.QuranContentTypeId}:{u.QuranLevelId}");
            parts.Add($"[{string.Join(",", unitSigs)}]");
        }

        return string.Join("_", parts);
    }

    private string GetSignatureFromDto(TeacherSubjectItemDto dto)
    {
        var parts = new List<string> { dto.SubjectId.ToString() };

        parts.Add(dto.CanTeachFullSubject ? "FULL" : "PARTIAL");

        if (dto.Units?.Any() == true)
        {
            var unitSigs = dto.Units
                .OrderBy(u => u.UnitId)
                .ThenBy(u => u.QuranContentTypeId ?? 0)
                .ThenBy(u => u.QuranLevelId ?? 0)
                .Select(u => $"{u.UnitId}:{u.QuranContentTypeId}:{u.QuranLevelId}");
            parts.Add($"[{string.Join(",", unitSigs)}]");
        }

        return string.Join("_", parts);
    }

    public async Task<List<int>> GetActiveTeacherIdsBySubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        return await _teacherSubjects
            .AsNoTracking()
            .Where(ts => ts.SubjectId == subjectId
                         && ts.IsActive
                         && ts.VerificationStatus == DocumentVerificationStatus.Approved
                         && ts.Teacher != null
                         && ts.Teacher.Status == TeacherStatus.Active)
            .Select(ts => ts.TeacherId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TeacherSubject>> GetAllByTeacherIdForAdminAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _teacherSubjects
            .AsNoTracking()
            .Where(ts => ts.TeacherId == teacherId)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranContentType)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranLevel)
            .OrderBy(ts => ts.Subject.NameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeacherSubject?> GetByIdForTeacherAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        return await _teacherSubjects
            .Where(ts => ts.Id == teacherSubjectId && ts.TeacherId == teacherId)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranContentType)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranLevel)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResult<TeacherSubject>> GetPagedForAdminAsync(
        int pageNumber,
        int pageSize,
        int? teacherId = null,
        int? subjectId = null,
        bool? isActive = null,
        DocumentVerificationStatus? verificationStatus = null,
        CancellationToken cancellationToken = default)
    {
        var query = _teacherSubjects
            .AsNoTracking()
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.Teacher)
                .ThenInclude(t => t.User)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.Unit)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranContentType)
            .Include(ts => ts.TeacherSubjectUnits)
                .ThenInclude(tsu => tsu.QuranLevel)
            .AsQueryable();

        if (teacherId.HasValue)
            query = query.Where(ts => ts.TeacherId == teacherId.Value);
        if (subjectId.HasValue)
            query = query.Where(ts => ts.SubjectId == subjectId.Value);
        if (isActive.HasValue)
            query = query.Where(ts => ts.IsActive == isActive.Value);
        if (verificationStatus.HasValue)
            query = query.Where(ts => ts.VerificationStatus == verificationStatus.Value);

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

    public Task<List<TeacherSubject>> GetSubjectsInDomainForCascadeRejectAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _teacherSubjects
            .Include(ts => ts.Subject)
            .Where(ts => ts.TeacherId == teacherId
                         && ts.Subject!.DomainId == domainId
                         && ts.VerificationStatus != DocumentVerificationStatus.Rejected)
            .ToListAsync(cancellationToken);

    public Task<List<TeacherSubject>> GetCascadeRejectedSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _teacherSubjects
            .Include(ts => ts.Subject)
            .Where(ts => ts.TeacherId == teacherId
                         && ts.Subject!.DomainId == domainId
                         && ts.VerificationStatus == DocumentVerificationStatus.Rejected
                         && ts.RejectionSource == TeacherSubjectRejectionSource.DomainQuestionCascade)
            .ToListAsync(cancellationToken);

    public Task<List<TeacherSubject>> GetDirectRejectedSubjectsAsync(int teacherId, CancellationToken cancellationToken = default) =>
        _teacherSubjects
            .AsNoTracking()
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Where(ts => ts.TeacherId == teacherId
                         && ts.VerificationStatus == DocumentVerificationStatus.Rejected
                         && ts.RejectionSource != TeacherSubjectRejectionSource.DomainQuestionCascade)
            .ToListAsync(cancellationToken);
}
