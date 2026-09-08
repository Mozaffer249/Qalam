using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class PaymentTransactionEventRepository : IPaymentTransactionEventRepository
{
    private readonly ApplicationDBContext _context;

    public PaymentTransactionEventRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PaymentTransactionEvent evt, CancellationToken cancellationToken = default)
    {
        _context.PaymentTransactionEvents.Add(evt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByProviderEventIdAsync(
        string paymentProvider,
        string providerEventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentProvider) || string.IsNullOrWhiteSpace(providerEventId))
            return Task.FromResult(false);

        return _context.PaymentTransactionEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.PaymentProvider == paymentProvider
                     && e.ProviderEventId == providerEventId
                     && e.EventType != PaymentTransactionEventType.WebhookReceived
                     && e.EventType != PaymentTransactionEventType.WebhookDuplicate,
                cancellationToken);
    }

    public Task<bool> ExistsByPayloadHashAsync(
        string paymentProvider,
        string payloadHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentProvider) || string.IsNullOrWhiteSpace(payloadHash))
            return Task.FromResult(false);

        return _context.PaymentTransactionEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.PaymentProvider == paymentProvider
                     && e.PayloadHash == payloadHash
                     && (e.EventType == PaymentTransactionEventType.WebhookProcessed
                         || e.EventType == PaymentTransactionEventType.StatusChanged
                         || e.EventType == PaymentTransactionEventType.RefundSucceeded
                         || e.EventType == PaymentTransactionEventType.ConfirmSucceeded),
                cancellationToken);
    }

    public Task<List<PaymentTransactionEvent>> ListForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        _context.PaymentTransactionEvents
            .AsNoTracking()
            .Where(e => e.PaymentId == paymentId)
            .OrderByDescending(e => e.ReceivedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

    public IQueryable<PaymentTransactionEvent> GetFilteredQuery(
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
        DateTime? toUtc = null)
    {
        var q = _context.PaymentTransactionEvents.AsNoTracking().AsQueryable();

        if (paymentId.HasValue)
            q = q.Where(e => e.PaymentId == paymentId);
        if (enrollmentId.HasValue)
            q = q.Where(e => e.EnrollmentId == enrollmentId);
        if (enrollmentRequestId.HasValue)
            q = q.Where(e => e.EnrollmentRequestId == enrollmentRequestId);
        if (openSessionRequestId.HasValue)
            q = q.Where(e => e.OpenSessionRequestId == openSessionRequestId);
        if (!string.IsNullOrWhiteSpace(provider))
            q = q.Where(e => e.PaymentProvider == provider);
        if (!string.IsNullOrWhiteSpace(providerPaymentId))
            q = q.Where(e => e.ProviderPaymentId == providerPaymentId);
        if (!string.IsNullOrWhiteSpace(providerInvoiceId))
            q = q.Where(e => e.ProviderInvoiceId == providerInvoiceId);
        if (source.HasValue)
            q = q.Where(e => e.Source == source);
        if (eventType.HasValue)
            q = q.Where(e => e.EventType == eventType);
        if (result.HasValue)
            q = q.Where(e => e.Result == result);
        if (fromUtc.HasValue)
            q = q.Where(e => e.ReceivedAt >= fromUtc);
        if (toUtc.HasValue)
            q = q.Where(e => e.ReceivedAt <= toUtc);

        return q.OrderByDescending(e => e.ReceivedAt).ThenByDescending(e => e.Id);
    }
}
