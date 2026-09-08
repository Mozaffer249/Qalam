using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// One Moyasar (or future gateway) reconciliation pass — manual or scheduled.
/// </summary>
public class PaymentReconciliationRun : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string PaymentProvider { get; set; } = "Moyasar";

    public PaymentReconciliationRunSource Source { get; set; }

    public PaymentReconciliationRunStatus Status { get; set; } = PaymentReconciliationRunStatus.Running;

    /// <summary>
    /// Unique key for scheduled runs (e.g. Moyasar:20260908T0215) to dedupe across API replicas.
    /// </summary>
    [MaxLength(80)]
    public string? ScheduleKey { get; set; }

    public DateTime LookbackFromUtc { get; set; }

    public DateTime LookbackToUtc { get; set; }

    public int? PaginationCursor { get; set; }

    public int RemotePaymentsSeen { get; set; }

    public int RemoteInvoicesSeen { get; set; }

    public int MatchedCount { get; set; }

    public int RepairedCount { get; set; }

    public int MismatchCount { get; set; }

    public int UnresolvedRemoteCount { get; set; }

    public int MissingRemoteCount { get; set; }

    [MaxLength(2000)]
    public string? ErrorSummary { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? TriggeredByUserId { get; set; }
}
