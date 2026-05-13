using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// طالب مشارك في تسجيل. للفردي: صف واحد. للجماعي: صف لكل عضو.
/// كل مشارك يدفع حصته بشكل مستقل.
/// </summary>
public class EnrollmentParticipant : AuditableEntity
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime? PaidAt { get; set; }

    // Navigation Properties
    public Enrollment Enrollment { get; set; } = null!;
    public Student.Student Student { get; set; } = null!;
    public ICollection<EnrollmentPayment> EnrollmentPayments { get; set; } = new List<EnrollmentPayment>();
}
