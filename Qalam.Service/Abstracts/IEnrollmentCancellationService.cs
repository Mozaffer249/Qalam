using Qalam.Data.DTOs.Policy;

namespace Qalam.Service.Abstracts;

public interface IEnrollmentCancellationService
{
    /// <summary>What cancelling the enrollment would refund under its policy version. Null when not found.</summary>
    Task<PolicyPreviewDto?> PreviewAsync(int enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an enrollment. Unpaid enrollments are cancelled directly; active ones go through the
    /// cancellation policy (before/after first session), which refunds and records a policy case.
    /// Throws <see cref="Models.Policy.PolicyDeniedException"/> when the policy does not allow it.
    /// </summary>
    /// <returns>The applied case, or null when no money was involved.</returns>
    Task<PolicyOutcomeDto?> CancelAsync(
        int enrollmentId,
        int cancelledByUserId,
        string? reason = null,
        CancellationToken cancellationToken = default,
        string actorRole = "Student");
}
