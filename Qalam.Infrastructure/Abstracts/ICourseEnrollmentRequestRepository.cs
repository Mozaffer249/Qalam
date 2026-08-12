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

    /// <summary>
    /// Received S1 invitations for the given students (any confirmation / request status),
    /// one row per invite member. Sets <see cref="StudentInvitationListItemDto.ParentStatus"/>.
    /// Does not set RespondByUtc or public media URLs.
    /// </summary>
    Task<List<StudentInvitationListItemDto>> GetReceivedInvitationListItemsAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sent S1 invitations for the caller: one row per request that has any Invited member
    /// (any confirmation / request status). Does not set RespondByUtc or public media URLs.
    /// </summary>
    Task<List<StudentInvitationListItemDto>> GetSentInvitationListItemsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
