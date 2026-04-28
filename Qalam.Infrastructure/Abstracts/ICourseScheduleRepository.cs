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
}
