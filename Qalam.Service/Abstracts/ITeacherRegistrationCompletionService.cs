using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationCompletionService
{
    /// <summary>
    /// Recomputes teacher status from active required submissions (and linked documents).
    /// Does not set Active — use <see cref="ActivateTeacherAccountAsync"/> for manual authorization.
    /// </summary>
    Task RefreshTeacherStatusAfterReviewAsync(int teacherId, CancellationToken cancellationToken = default);

    Task SyncSubmissionStatusFromDocumentAsync(
        int teacherDocumentId,
        DocumentVerificationStatus status,
        int? reviewedByAdminId,
        string? rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when all required documents/submissions are approved and every subject is approved (≥1 subject).
    /// </summary>
    Task<bool> CanActivateTeacherAccountAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually authorizes a teacher account when <see cref="CanActivateTeacherAccountAsync"/> is true.
  /// </summary>
    /// <returns>Success flag and error message when not eligible.</returns>
    Task<(bool Success, string? ErrorMessage)> ActivateTeacherAccountAsync(
        int teacherId,
        int adminId,
        CancellationToken cancellationToken = default);
}
