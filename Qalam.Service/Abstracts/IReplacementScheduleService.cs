using Qalam.Data.Entity.Common.Enums;
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

    /// <summary>
    /// Moves <paramref name="original"/> (tracked) to a teacher slot: it becomes Rescheduled and a new session
    /// replaces it. Throws <see cref="InvalidOperationException"/> when the slot is inactive, not the teacher's,
    /// in the past, or already booked.
    /// </summary>
    Task<CourseSchedule> RescheduleAsync(
        CourseSchedule original,
        int teacherId,
        DateOnly date,
        int teacherAvailabilityId,
        ScheduleCancellationReason reason,
        int? policyCaseId,
        string note,
        CancellationToken cancellationToken = default);
}
