using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Payment;

public class PayoutBatch : AuditableEntity
{
    public int Id { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal TotalAmount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public PayoutBatchStatus Status { get; set; } = PayoutBatchStatus.Pending;

    [MaxLength(120)]
    public string? MockTransferRef { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? FailedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? ApprovedByUserId { get; set; }

    public int? ProcessedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<PayoutItem> Items { get; set; } = new List<PayoutItem>();
}
