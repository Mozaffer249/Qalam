using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// ربط الدفع بتسجيل الدورة
/// </summary>
public class CourseEnrollmentPayment : AuditableEntity
{
    public int Id { get; set; }
    
    public int CourseEnrollmentId { get; set; }
    public int PaymentId { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    // Navigation Properties
    public Course.CourseEnrollment CourseEnrollment { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
