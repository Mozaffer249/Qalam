using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Payment;

/// <summary>
/// Body for paying an enrollment. Single payer (request owner) pays full AmountDue;
/// [ParticipantId] may be any participant on that enrollment.
/// </summary>
public class PayEnrollmentParticipantRequestDto
{
    /// <summary>
    /// Primary key of <c>EnrollmentParticipant</c> — found on request detail
    /// <c>payParticipantId</c> or GET Student/Enrollments.
    /// </summary>
    public int ParticipantId { get; set; }
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

    /// <summary>True when the parent enrollment is now Active (single full payment).</summary>
    public bool EnrollmentActivated { get; set; }

    /// <summary>Number of CourseSchedule rows generated as part of this payment (0 unless activation happened).</summary>
    public int SchedulesCreated { get; set; }
}

public class EnrollmentParticipantPaymentSummaryDto
{
    public int ParticipantId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Share { get; set; }
}

/// <summary>
/// Unified payment summary. Individual: one participant; Group: one per member.
/// </summary>
public class EnrollmentPaymentSummaryDto
{
    public int EnrollmentId { get; set; }
    public EnrollmentKind Kind { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string Currency { get; set; } = "SAR";
    public List<EnrollmentParticipantPaymentSummaryDto> Participants { get; set; } = new();
}
