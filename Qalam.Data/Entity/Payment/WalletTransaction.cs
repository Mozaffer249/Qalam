using System.ComponentModel.DataAnnotations;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Immutable wallet ledger row. <see cref="Amount"/> is signed: positive credits, negative debits.
/// </summary>
public class WalletTransaction
{
    public int Id { get; set; }

    public int WalletId { get; set; }

    public WalletTransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Completed;

    /// <summary>Compensating row that reversed this one (status then becomes Reversed).</summary>
    public int? ReversedByTransactionId { get; set; }

    public int? PolicyCaseId { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public int? PaymentId { get; set; }

    public int? RefundId { get; set; }

    public int? EnrollmentId { get; set; }

    public int? CourseScheduleId { get; set; }

    public int? ComplaintId { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    [MaxLength(64)]
    public string? ReasonCode { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentWallet Wallet { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Refund? Refund { get; set; }
    public Course.Enrollment? Enrollment { get; set; }
}
