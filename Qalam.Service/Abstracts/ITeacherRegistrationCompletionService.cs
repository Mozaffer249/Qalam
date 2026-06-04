using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationCompletionService
{
    /// <summary>
    /// Recomputes teacher status from active required submissions (and linked documents).
    /// </summary>
    Task RefreshTeacherStatusAfterReviewAsync(int teacherId, CancellationToken cancellationToken = default);

    Task SyncSubmissionStatusFromDocumentAsync(
        int teacherDocumentId,
        DocumentVerificationStatus status,
        int? reviewedByAdminId,
        string? rejectionReason,
        CancellationToken cancellationToken = default);
}
