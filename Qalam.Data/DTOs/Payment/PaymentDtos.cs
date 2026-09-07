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
/// Result of a successful payment confirmation.
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

/// <summary>Body for creating a Moyasar payment intent.</summary>
public class CreatePaymentIntentRequestDto
{
    public int ParticipantId { get; set; }
}

/// <summary>Client checkout payload — amount is fixed by the backend via givenId.</summary>
public class PaymentIntentDto
{
    public int PaymentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public PaymentClientMode ClientMode { get; set; }
    public string GivenId { get; set; } = string.Empty;
    public int AmountHalalas { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? PublishableApiKey { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ClientSecret { get; set; }
    public string? CallbackUrl { get; set; }
    public string? ApplePayMerchantId { get; set; }
    public string? ApplePayLabel { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>Paginated student payment history row.</summary>
public class StudentPaymentListItemDto
{
    public int PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime? PaidAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? EnrollmentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public decimal RefundedAmount { get; set; }
}

public class PaymentReceiptItemDto
{
    public string ItemType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentReceiptRefundDto
{
    public int RefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Student-facing payment receipt detail.</summary>
public class StudentPaymentReceiptDto
{
    public int PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime? PaidAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? EnrollmentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? CourseTitle { get; set; }
    public string? TeacherName { get; set; }
    public List<PaymentReceiptItemDto> Items { get; set; } = new();
    public List<PaymentReceiptRefundDto> Refunds { get; set; } = new();
}

/// <summary>Body for confirming a Moyasar payment after 3DS / SDK result.</summary>
public class ConfirmPaymentRequestDto
{
    public string GivenId { get; set; } = string.Empty;
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
