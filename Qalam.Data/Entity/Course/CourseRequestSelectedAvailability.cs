using Qalam.Data.Commons;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// أوقات التوفر المختارة في طلب التسجيل
/// </summary>
public class CourseRequestSelectedAvailability : AuditableEntity
{
    public int Id { get; set; }
    
    public int CourseEnrollmentRequestId { get; set; }
    public int TeacherAvailabilityId { get; set; }
    
    /// <summary>
    /// ترتيب الأولوية (اختياري)
    /// </summary>
    public int? PriorityOrder { get; set; }
    
    // Navigation Properties
    public CourseEnrollmentRequest CourseEnrollmentRequest { get; set; } = null!;
    public TeacherAvailability TeacherAvailability { get; set; } = null!;
}
