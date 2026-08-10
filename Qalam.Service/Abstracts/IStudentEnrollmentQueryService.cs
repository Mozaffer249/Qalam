using Qalam.Data.DTOs.Course;

namespace Qalam.Service.Abstracts;

public interface IStudentEnrollmentQueryService
{
    /// <summary>
    /// Paged enrollments for a student, with next-session / progress enrichment.
    /// </summary>
    Task<(List<EnrollmentListItemDto> Items, int TotalCount)> ListForStudentAsync(
        int studentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
