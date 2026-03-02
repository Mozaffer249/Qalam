using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

public class CourseGroupEnrollmentMember : AuditableEntity
{
    public int Id { get; set; }
    public int CourseGroupEnrollmentId { get; set; }
    public int StudentId { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }

    public CourseGroupEnrollment CourseGroupEnrollment { get; set; } = null!;
    public Student.Student Student { get; set; } = null!;
    public ICollection<GroupEnrollmentMemberPayment> GroupEnrollmentMemberPayments { get; set; } = new List<GroupEnrollmentMemberPayment>();
}
