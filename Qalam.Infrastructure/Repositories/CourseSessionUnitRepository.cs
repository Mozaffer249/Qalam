using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseSessionUnitRepository : GenericRepositoryAsync<CourseSessionUnit>, ICourseSessionUnitRepository
{
    private readonly ApplicationDBContext _context;

    public CourseSessionUnitRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<int?> GetSubjectIdForOwnedSessionAsync(int sessionId, int courseId, int teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId
                        && s.CourseId == courseId
                        && s.Course.TeacherId == teacherId
                        && s.Course.TeacherSubject != null)
            .Select(s => (int?)s.Course.TeacherSubject!.SubjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> ReplaceUnitsAsync(int sessionId, IEnumerable<CourseSessionUnit> newUnits, CancellationToken cancellationToken = default)
    {
        await _context.CourseSessionUnits
            .Where(u => u.CourseSessionId == sessionId)
            .ExecuteDeleteAsync(cancellationToken);

        var list = newUnits.ToList();
        if (list.Count > 0)
        {
            await _context.CourseSessionUnits.AddRangeAsync(list, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return list.Count;
    }

    public async Task<List<CourseSessionUnitDto>> GetHydratedDtosBySessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseSessionUnits
            .AsNoTracking()
            .Where(u => u.CourseSessionId == sessionId)
            .Select(u => new CourseSessionUnitDto
            {
                Id = u.Id,
                ContentUnitId = u.ContentUnitId,
                ContentUnitNameEn = u.ContentUnit != null ? u.ContentUnit.NameEn : null,
                ContentUnitNameAr = u.ContentUnit != null ? u.ContentUnit.NameAr : null,
                LessonId = u.LessonId,
                LessonNameEn = u.Lesson != null ? u.Lesson.NameEn : null,
                LessonNameAr = u.Lesson != null ? u.Lesson.NameAr : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ValidateUnitsBelongToSubjectAsync(
        IReadOnlyCollection<int> contentUnitIds,
        IReadOnlyCollection<int> lessonIds,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        if (contentUnitIds.Count > 0)
        {
            var unitRows = await _context.ContentUnits
                .AsNoTracking()
                .Where(c => contentUnitIds.Contains(c.Id))
                .Select(c => new { c.Id, c.SubjectId })
                .ToListAsync(cancellationToken);

            var missing = contentUnitIds.Except(unitRows.Select(r => r.Id)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException($"ContentUnit(s) not found: {string.Join(", ", missing)}.");

            if (unitRows.Any(r => r.SubjectId != subjectId))
                throw new InvalidOperationException("Selected units/lessons must belong to the course's subject.");
        }

        if (lessonIds.Count > 0)
        {
            var lessonRows = await _context.Lessons
                .AsNoTracking()
                .Where(l => lessonIds.Contains(l.Id))
                .Select(l => new { l.Id, l.Unit!.SubjectId })
                .ToListAsync(cancellationToken);

            var missing = lessonIds.Except(lessonRows.Select(r => r.Id)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException($"Lesson(s) not found: {string.Join(", ", missing)}.");

            if (lessonRows.Any(r => r.SubjectId != subjectId))
                throw new InvalidOperationException("Selected units/lessons must belong to the course's subject.");
        }
    }
}
