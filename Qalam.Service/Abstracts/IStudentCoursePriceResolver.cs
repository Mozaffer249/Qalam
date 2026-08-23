using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface IStudentCoursePriceResolver
{
    /// <summary>
    /// Student-facing package total for a course (custom rate when reflected, else platform rate).
    /// </summary>
    Task<decimal> ResolveCourseTotalPriceAsync(
        Course course,
        int viewerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="ResolveCourseTotalPriceAsync(Course,int,CancellationToken)"/> when duration is already known (e.g. catalog projections).
    /// </summary>
    Task<decimal> ResolveCourseTotalPriceAsync(
        int domainId,
        string sessionTypeCode,
        int teacherId,
        int totalMinutes,
        decimal storedHourlyPrice,
        int viewerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Display price for an enrollment: snapshot → request estimate → amount due → live estimate.
    /// </summary>
    Task<decimal> ResolveEnrollmentCoursePriceAsync(
        Enrollment enrollment,
        int viewerUserId,
        CancellationToken cancellationToken = default);
}
