using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface ILrsDispatchService
{
    Task DispatchSessionCompletedAsync(CourseSchedule schedule, CancellationToken ct = default);
    Task DispatchSessionLaunchedAsync(CourseSchedule schedule, CancellationToken ct = default);
    Task DispatchStudentAttendedAsync(int studentId, CourseSchedule schedule, CancellationToken ct = default);
    Task DispatchEnrollmentActivatedAsync(Enrollment enrollment, CancellationToken ct = default);
    Task DispatchProgressUpdatedAsync(CourseSchedule schedule, CancellationToken ct = default);
    Task DispatchCourseCompletedAsync(Enrollment enrollment, CancellationToken ct = default);
    Task<int> ReplayFullJourneyAsync(int enrollmentId, CancellationToken ct = default);
}
