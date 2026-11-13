using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface ILrsDispatchService
{
    Task DispatchEnrollmentActivatedAsync(Enrollment enrollment, CancellationToken ct = default);
    Task DispatchProgressUpdatedAsync(CourseSchedule schedule, CancellationToken ct = default);
    Task DispatchCourseCompletedAsync(Enrollment enrollment, CancellationToken ct = default);
    Task DispatchStudentRatedAsync(int studentId, int courseScheduleId, int rating, string? feedback, CancellationToken ct = default);
    Task<int> ReplayFullJourneyAsync(int enrollmentId, CancellationToken ct = default);
}
