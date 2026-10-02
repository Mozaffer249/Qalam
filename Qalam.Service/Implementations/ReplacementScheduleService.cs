using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ReplacementScheduleService : IReplacementScheduleService
{
    private readonly ApplicationDBContext _db;

    public ReplacementScheduleService(ApplicationDBContext db) => _db = db;

    public async Task<CourseSchedule> CreateAsync(
        CourseSchedule source,
        string note,
        DateOnly? date = null,
        int? teacherAvailabilityId = null,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var replacement = new CourseSchedule
        {
            EnrollmentId = source.EnrollmentId,
            CourseSessionId = source.CourseSessionId,
            Date = date ?? (source.Date > today ? source.Date : today.AddDays(7)),
            TeacherAvailabilityId = teacherAvailabilityId ?? source.TeacherAvailabilityId,
            DurationMinutes = durationMinutes is > 0 ? durationMinutes.Value : source.DurationMinutes,
            TeachingModeId = source.TeachingModeId,
            LocationId = source.LocationId,
            Status = ScheduleStatus.Scheduled,
            TeacherNote = note.Length > 500 ? note[..500] : note,
            ReplacesScheduleId = source.Id,
            CreatedAt = DateTime.UtcNow,
        };

        _db.CourseSchedules.Add(replacement);
        await _db.SaveChangesAsync(cancellationToken);
        return replacement;
    }
}
