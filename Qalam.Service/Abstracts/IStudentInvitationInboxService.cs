using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface IStudentInvitationInboxService
{
    /// <summary>
    /// Unified invitation inbox (S1 course + S2 OSR): received invites for visible students
    /// (adult self / guardian children) plus the caller's sent requests (one row each),
    /// filtered by <paramref name="scope"/>. Child accounts see none for themselves.
    /// Same parent as invitee + owner → invitee row only.
    /// </summary>
    Task<StudentInvitationListResultDto> GetMyInvitationsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        InvitationInboxScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invitation detail for S1 or OSR by <c>invitationKey</c> (<c>EnrollmentRequest-901</c> /
    /// <c>OpenSessionRequest-44</c>). Null when the key is invalid, not found, or caller cannot view it.
    /// </summary>
    Task<StudentInvitationDetailDto?> GetInvitationDetailAsync(
        int userId,
        string invitationKey,
        CancellationToken cancellationToken = default);
}
