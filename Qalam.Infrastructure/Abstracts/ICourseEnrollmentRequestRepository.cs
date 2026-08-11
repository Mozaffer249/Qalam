using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseEnrollmentRequestRepository : IGenericRepositoryAsync<CourseEnrollmentRequest>
{
    IQueryable<CourseEnrollmentRequest> GetByUserIdQueryable(int userId);
    IQueryable<CourseEnrollmentRequest> GetByCourseIdQueryable(int courseId);
    IQueryable<CourseRequestGroupMember> GetPendingInvitationsForStudentsQueryable(List<int> studentIds);

    /// <summary>
    /// Pending Invited group members for the given students (S1 inbox), projected to list DTOs.
    /// Does not set <see cref="StudentInvitationListItemDto.RespondByUtc"/> or public media URLs.
    /// </summary>
    Task<List<StudentInvitationListItemDto>> GetPendingInvitationListItemsAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken = default);
}
