using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

public class CourseGroupEnrollment : AuditableEntity
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int EnrollmentRequestId { get; set; }
    public int LeaderStudentId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.PendingPayment;
    public DateTime? ActivatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public CourseEnrollmentRequest EnrollmentRequest { get; set; } = null!;
    public Student.Student LeaderStudent { get; set; } = null!;
    public ICollection<CourseGroupEnrollmentMember> Members { get; set; } = new List<CourseGroupEnrollmentMember>();
    public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
}
