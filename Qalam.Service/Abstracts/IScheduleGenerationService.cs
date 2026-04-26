using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface IScheduleGenerationService
{
    /// <summary>
    /// Materialises CourseSchedule rows for an enrollment that just became fully paid.
    /// Caller is responsible for attaching the returned schedules to the appropriate
    /// enrollment navigation collection and saving the DbContext.
    /// </summary>
    List<CourseSchedule> Generate(
        Course course,
        CourseEnrollmentRequest request,
        int? courseEnrollmentId,
        int? courseGroupEnrollmentId,
        DateOnly startDate);
}
