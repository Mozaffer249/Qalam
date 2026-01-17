using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// تسجيل فعلي في دورة (بعد الموافقة)
/// </summary>
public class CourseEnrollment : AuditableEntity
{
    public int Id { get; set; }
    
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    
    /// <summary>
    /// معرف المعلم الذي وافق
    /// </summary>
    public int ApprovedByTeacherId { get; set; }
    
    public DateTime ApprovedAt { get; set; }
    
    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.PendingPayment;
    
    // Navigation Properties
    public Course Course { get; set; } = null!;
    public Student.Student Student { get; set; } = null!;
    public Teacher.Teacher ApprovedByTeacher { get; set; } = null!;
    
    public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
    public ICollection<CourseEnrollmentPayment> CourseEnrollmentPayments { get; set; } = new List<CourseEnrollmentPayment>();
}
