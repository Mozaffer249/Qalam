using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;

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
        CancellationToken cancellationToken = default,
        EnrollmentStatus? status = null);

    /// <summary>
    /// Paged enrollments where any participant is in <paramref name="studentIds"/>.
    /// <paramref name="ownedStudentIdsForProjection"/> controls which participants appear in
    /// <see cref="EnrollmentListItemDto.EnrolledStudents"/> (caller-owned subset).
    /// When <paramref name="status"/> is set, only that enrollment status is returned.
    /// </summary>
    Task<(List<EnrollmentListItemDto> Items, int TotalCount)> ListForStudentsAsync(
        IReadOnlyCollection<int> studentIds,
        IReadOnlyCollection<int> ownedStudentIdsForProjection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default,
        EnrollmentStatus? status = null);
}
