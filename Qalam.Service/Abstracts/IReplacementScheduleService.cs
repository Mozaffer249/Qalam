using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface IReplacementScheduleService
{
    /// <summary>
    /// Adds and saves a Scheduled session that replaces <paramref name="source"/> (same enrollment, mode,
    /// location and duration). Without a date/slot it reuses the source slot on its date, or a week from now.
    /// </summary>
    Task<CourseSchedule> CreateAsync(
        CourseSchedule source,
        string note,
        DateOnly? date = null,
        int? teacherAvailabilityId = null,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default);
}
