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

    /// <summary>
    /// Paged enrollments where any participant is in <paramref name="studentIds"/>.
    /// <paramref name="ownedStudentIdsForProjection"/> controls which participants appear in
    /// <see cref="EnrollmentListItemDto.EnrolledStudents"/> (caller-owned subset).
    /// </summary>
    Task<(List<EnrollmentListItemDto> Items, int TotalCount)> ListForStudentsAsync(
        IReadOnlyCollection<int> studentIds,
        IReadOnlyCollection<int> ownedStudentIdsForProjection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
