using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Payment;

/// <summary>
/// Body for paying an individual enrollment.
/// </summary>
public class PayEnrollmentRequestDto
{
    /// <summary>
    /// The CourseEnrollment to pay for. Required.
    /// </summary>
    public int EnrollmentId { get; set; }
}

/// <summary>
/// Body for paying for a single member of a group enrollment.
/// </summary>
public class PayGroupMemberRequestDto
{
    public int GroupEnrollmentId { get; set; }

    /// <summary>
    /// The student that this payment is for (member being paid for, not the payer).
    /// The caller (= payer) is resolved from auth: self if adult, guardian if minor.
    /// </summary>
    public int StudentId { get; set; }
}

/// <summary>
/// Result of a successful (mock) payment.
/// </summary>
public class PaymentResultDto
{
    public int PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime PaidAt { get; set; }

    /// <summary>True when the parent enrollment is now Active (group: all members paid).</summary>
    public bool EnrollmentActivated { get; set; }

    /// <summary>Number of CourseSchedule rows generated as part of this payment (0 unless activation happened).</summary>
    public int SchedulesCreated { get; set; }
}

/// <summary>
/// Payment summary for an individual enrollment.
/// </summary>
public class EnrollmentPaymentSummaryDto
{
    public int EnrollmentId { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public string Currency { get; set; } = "SAR";
}

public class GroupEnrollmentMemberPaymentSummaryDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Share { get; set; }
}

/// <summary>
/// Payment summary for a group enrollment.
/// </summary>
public class GroupEnrollmentPaymentSummaryDto
{
    public int GroupEnrollmentId { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string Currency { get; set; } = "SAR";
    public List<GroupEnrollmentMemberPaymentSummaryDto> Members { get; set; } = new();
}
