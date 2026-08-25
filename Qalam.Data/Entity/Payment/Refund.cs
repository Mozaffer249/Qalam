using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Refund against a succeeded payment (mock provider in v1).
/// </summary>
public class Refund : AuditableEntity
{
    public int Id { get; set; }

    public int PaymentId { get; set; }

    public int EnrollmentId { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    [Required, MaxLength(500)]
    public string Reason { get; set; } = null!;

    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    [MaxLength(120)]
    public string? ProviderRefundId { get; set; }

    public int? InitiatedByUserId { get; set; }

    public Payment Payment { get; set; } = null!;
    public Course.Enrollment Enrollment { get; set; } = null!;
    public User? InitiatedByUser { get; set; }
}
