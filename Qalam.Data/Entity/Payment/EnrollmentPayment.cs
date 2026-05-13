using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// ربط الدفع بمشاركة طالب في تسجيل (يحل محل CourseEnrollmentPayment و GroupEnrollmentMemberPayment).
/// </summary>
public class EnrollmentPayment : AuditableEntity
{
    public int Id { get; set; }

    public int EnrollmentParticipantId { get; set; }

    public int PaymentId { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Navigation Properties
    public Course.EnrollmentParticipant EnrollmentParticipant { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
