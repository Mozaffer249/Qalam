namespace Qalam.Service.Abstracts;

/// <summary>
/// Reverts a PaymentPending OSR enrollment when payment hits a schedule conflict race,
/// restoring sibling offers and returning the request to an actionable state.
/// </summary>
public interface IOpenSessionRequestReleaseService
{
    Task ReleaseAfterPaymentConflictAsync(int enrollmentId, CancellationToken cancellationToken = default);
}
