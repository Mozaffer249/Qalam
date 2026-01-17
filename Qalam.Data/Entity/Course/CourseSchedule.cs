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
    
    public int CourseEnrollmentId { get; set; }
    
    /// <summary>
    /// تاريخ الجلسة
    /// </summary>
    public DateOnly Date { get; set; }
    
    public int TeacherAvailabilityId { get; set; }
    
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
    public CourseEnrollment CourseEnrollment { get; set; } = null!;
    public TeacherAvailability TeacherAvailability { get; set; } = null!;
    public TeachingMode TeachingMode { get; set; } = null!;
    public Location? Location { get; set; }
}
