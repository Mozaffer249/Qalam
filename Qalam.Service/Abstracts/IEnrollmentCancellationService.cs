namespace Qalam.Service.Abstracts;

public interface IEnrollmentCancellationService
{
    /// <summary>
    /// Cancels enrollment (PendingPayment or Active before first session), cancels open schedules,
    /// issues mock refunds when paid, restores free-trial flag when applicable.
    /// </summary>
    Task CancelAsync(
        int enrollmentId,
        int cancelledByUserId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
