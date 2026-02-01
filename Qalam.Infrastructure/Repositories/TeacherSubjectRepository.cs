using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
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
            .Where(ts => ts.TeacherId == teacherId && ts.IsActive)
            .Include(ts => ts.Subject)
                .ThenInclude(s => s.Domain)
            .Include(ts => ts.Curriculum)
            .Include(ts => ts.Level)
            .Include(ts => ts.Grade)
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
            .Include(ts => ts.Curriculum)
            .Include(ts => ts.Level)
            .Include(ts => ts.Grade)
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
                CurriculumId = subjectDto.CurriculumId,
                LevelId = subjectDto.LevelId,
                GradeId = subjectDto.GradeId,
                CanTeachFullSubject = subjectDto.CanTeachFullSubject,
                IsActive = true,
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
            .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId && ts.IsActive);
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
}
