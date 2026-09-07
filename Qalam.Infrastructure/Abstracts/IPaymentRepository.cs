using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IPaymentRepository : IGenericRepositoryAsync<Payment>
{
    Task<Payment?> GetByIdWithItemsAsync(int paymentId, CancellationToken cancellationToken = default);

    Task<Payment?> GetByProviderTransactionIdAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Newest Pending payment for an enrollment under the given provider, if any.
    /// </summary>
    Task<Payment?> GetOpenIntentForEnrollmentAsync(
        int enrollmentId,
        string provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel all Pending payments for an enrollment under the given provider.
    /// Preserves ProviderTransactionId so late webhooks can still resolve.
    /// </summary>
    Task CancelOpenIntentsForEnrollmentAsync(
        int enrollmentId,
        string provider,
        CancellationToken cancellationToken = default);

    IQueryable<Payment> GetPayerHistoryQuery(int payerUserId);

    Task<Payment?> GetReceiptAsync(int paymentId, CancellationToken cancellationToken = default);
}
