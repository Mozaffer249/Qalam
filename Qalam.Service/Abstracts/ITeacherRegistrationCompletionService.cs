using Qalam.Data.DTOs.Admin;
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
    /// True when all required registration submissions and domain question answers are approved.
    /// Subjects are not required for activation.
    /// </summary>
    Task<bool> CanActivateTeacherAccountAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Human-readable reasons why the teacher cannot be activated (empty when ready).
    /// </summary>
    Task<IReadOnlyList<string>> GetActivationBlockReasonsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when any active required registration requirement has no submission yet
    /// (e.g. catalog fields added after the teacher first registered).
    /// </summary>
    Task<bool> HasMissingRequiredRegistrationSubmissionsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when any required registration submission is still pending admin review.
    /// Missing submissions are not pending — use <see cref="HasMissingRequiredRegistrationSubmissionsAsync"/>.
    /// </summary>
    Task<bool> HasPendingRequiredRegistrationReviewAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually authorizes a teacher account when <see cref="CanActivateTeacherAccountAsync"/> is true.
    /// </summary>
    /// <returns>Success flag and error message when not eligible.</returns>
    Task<(bool Success, string? ErrorMessage)> ActivateTeacherAccountAsync(
        int teacherId,
        int adminId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the teacher has at least one approved domain and at least one non-approved domain with rejected submissions.
    /// </summary>
    Task<bool> HasPartialDomainReviewOutcomeAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// PendingVerification teachers eligible for bulk activation: partial domain outcome and <see cref="CanActivateTeacherAccountAsync"/> true.
    /// </summary>
    Task<IReadOnlyList<PartialDomainActivationCandidateDto>> GetPartialDomainActivationCandidatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates every partial-domain candidate via <see cref="ActivateTeacherAccountAsync"/>.
    /// </summary>
    Task<BulkActivatePartialDomainTeachersResultDto> BulkActivatePartialDomainTeachersAsync(
        int adminId,
        CancellationToken cancellationToken = default);
}
