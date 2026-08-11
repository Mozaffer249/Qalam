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
}
