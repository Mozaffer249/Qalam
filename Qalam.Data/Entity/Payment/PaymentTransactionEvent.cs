using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Append-only audit trail for payment lifecycle (intent, webhook, confirm, refund, reconciliation).
/// </summary>
public class PaymentTransactionEvent : AuditableEntity
{
    public long Id { get; set; }

    public int? PaymentId { get; set; }

    public int? EnrollmentId { get; set; }

    public int? EnrollmentParticipantId { get; set; }

    public int? EnrollmentRequestId { get; set; }

    public int? OpenSessionRequestId { get; set; }

    [Required, MaxLength(40)]
    public string PaymentProvider { get; set; } = string.Empty;

    public PaymentTransactionEventSource Source { get; set; }

    public PaymentTransactionEventType EventType { get; set; }

    public PaymentTransactionEventResult Result { get; set; }

    public PaymentStatus? StatusBefore { get; set; }

    public PaymentStatus? StatusAfter { get; set; }

    public decimal? Amount { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    [MaxLength(120)]
    public string? ProviderPaymentId { get; set; }

    [MaxLength(120)]
    public string? ProviderInvoiceId { get; set; }

    [MaxLength(120)]
    public string? ProviderEventId { get; set; }

    [MaxLength(80)]
    public string? CorrelationId { get; set; }

    [MaxLength(64)]
    public string? PayloadHash { get; set; }

    [MaxLength(8000)]
    public string? PayloadJson { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public Payment? Payment { get; set; }
}
