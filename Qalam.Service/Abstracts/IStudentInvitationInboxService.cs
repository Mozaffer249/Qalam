using Qalam.Data.DTOs.Course;

namespace Qalam.Service.Abstracts;

public interface IStudentInvitationInboxService
{
    /// <summary>
    /// Unified pending invitation inbox (S1 course + S2 OSR) for the caller's visible students:
    /// adult self (no GuardianId) and/or guardian children. Child accounts see none for themselves.
    /// </summary>
    Task<StudentInvitationListResultDto> GetMyInvitationsAsync(
        int userId,
        int pageNumber,
        int pageSize,
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
