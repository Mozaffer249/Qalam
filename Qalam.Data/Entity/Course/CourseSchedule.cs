using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// جدول جلسات الدورة المحددة
/// </summary>
public class CourseSchedule : AuditableEntity
{
    public int Id { get; set; }
    
    public int? CourseEnrollmentId { get; set; }
    public int? CourseGroupEnrollmentId { get; set; }
    
    /// <summary>
    /// تاريخ الجلسة
    /// </summary>
    public DateOnly Date { get; set; }
    
    public int TeacherAvailabilityId { get; set; }
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// طريقة التدريس
    /// </summary>
    public int TeachingModeId { get; set; }
    
    /// <summary>
    /// الموقع (مطلوب للتدريس الحضوري)
    /// </summary>
    public int? LocationId { get; set; }
    
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    
    // Navigation Properties
    public CourseEnrollment? CourseEnrollment { get; set; }
    public CourseGroupEnrollment? CourseGroupEnrollment { get; set; }
    public TeacherAvailability TeacherAvailability { get; set; } = null!;
    public TeachingMode TeachingMode { get; set; } = null!;
    public Location? Location { get; set; }
}
