using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface IPaymentTransactionEventRepository
{
    Task AddAsync(PaymentTransactionEvent evt, CancellationToken cancellationToken = default);

    Task<bool> ExistsByProviderEventIdAsync(
        string paymentProvider,
        string providerEventId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByPayloadHashAsync(
        string paymentProvider,
        string payloadHash,
        CancellationToken cancellationToken = default);

    Task<List<PaymentTransactionEvent>> ListForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    IQueryable<PaymentTransactionEvent> GetFilteredQuery(
        int? paymentId = null,
        int? enrollmentId = null,
        int? enrollmentRequestId = null,
        int? openSessionRequestId = null,
        string? provider = null,
        string? providerPaymentId = null,
        string? providerInvoiceId = null,
        PaymentTransactionEventSource? source = null,
        PaymentTransactionEventType? eventType = null,
        PaymentTransactionEventResult? result = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null);
}
