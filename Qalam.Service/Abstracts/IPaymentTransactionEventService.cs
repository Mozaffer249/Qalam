using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

public interface IPaymentTransactionEventService
{
    Task<PaymentTransactionEvent> RecordAsync(
        PaymentTransactionEventRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> IsDuplicateDeliveryAsync(
        string paymentProvider,
        string? providerEventId,
        string? payloadHash,
        CancellationToken cancellationToken = default);

    Task EnrichLinksFromPaymentAsync(
        PaymentTransactionEventRequest request,
        Payment payment,
        CancellationToken cancellationToken = default);

    Task<List<PaymentTransactionEvent>> ListForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    string? ComputePayloadHash(string? rawBody);

    string? SanitizePayload(string? rawBody);
}

public sealed class PaymentTransactionEventRequest
{
    public int? PaymentId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? EnrollmentParticipantId { get; set; }
    public int? EnrollmentRequestId { get; set; }
    public int? OpenSessionRequestId { get; set; }
    public string PaymentProvider { get; set; } = string.Empty;
    public PaymentTransactionEventSource Source { get; set; }
    public PaymentTransactionEventType EventType { get; set; }
    public PaymentTransactionEventResult Result { get; set; } = PaymentTransactionEventResult.Success;
    public PaymentStatus? StatusBefore { get; set; }
    public PaymentStatus? StatusAfter { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderInvoiceId { get; set; }
    public string? ProviderEventId { get; set; }
    public string? CorrelationId { get; set; }
    public string? PayloadHash { get; set; }
    public string? RawPayload { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
