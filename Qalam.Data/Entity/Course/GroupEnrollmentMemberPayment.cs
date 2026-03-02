using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

public class GroupEnrollmentMemberPayment : AuditableEntity
{
    public int Id { get; set; }
    public int CourseGroupEnrollmentMemberId { get; set; }
    public int PaymentId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public CourseGroupEnrollmentMember CourseGroupEnrollmentMember { get; set; } = null!;
    public Qalam.Data.Entity.Payment.Payment Payment { get; set; } = null!;
}
