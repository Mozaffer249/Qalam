using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseScheduleRepository : IGenericRepositoryAsync<CourseSchedule>
{
    /// <summary>
    /// Returns the (Date, TeacherAvailabilityId) pairs already booked (Status = Scheduled)
    /// within the given window for any of the supplied availability ids. Used at submit time
    /// for conflict detection and at payment time for race-loser handling.
    /// </summary>
    Task<HashSet<(DateOnly Date, int TeacherAvailabilityId)>> GetScheduledSlotsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyCollection<int> teacherAvailabilityIds,
        CancellationToken ct);

    /// <summary>
    /// Booked-slot lookup for the availability-match endpoint: returns the date + time window of
    /// every Scheduled CourseSchedule the teacher already owns within [from, to]. Used to flag
    /// conflicts on a student's proposed session timing.
    /// </summary>
    Task<List<(DateOnly Date, TimeSpan Start, TimeSpan End)>> GetTeacherBookedSlotsInRangeAsync(
        int teacherId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracking load with Enrollment.Participants, Attendances, and TimeSlot for lifecycle operations.
    /// </summary>
    Task<CourseSchedule?> GetByIdForLifecycleAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules still Scheduled/InProgress whose (Date + EndTime + grace) is before <paramref name="utcNow"/>.
    /// Returned tracked with Participants + Attendances for auto-complete.
    /// </summary>
    Task<List<CourseSchedule>> GetOverdueForAutoCompleteAsync(
        DateTime utcNow,
        int graceMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scheduled sessions whose platform-local start has passed and end+grace has not yet passed.
    /// Returned tracked for auto InProgress.
    /// </summary>
    Task<List<CourseSchedule>> GetDueForAutoStartAsync(
        DateTime utcNow,
        int graceMinutes,
        CancellationToken cancellationToken = default);
}
