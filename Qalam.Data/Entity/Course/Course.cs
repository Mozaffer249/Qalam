using System.ComponentModel.DataAnnotations.Schema;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// الدورة التعليمية
/// </summary>
public class Course : AuditableEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// عنوان الدورة
    /// </summary>
    public string Title { get; set; } = default!;
    
    /// <summary>
    /// وصف الدورة
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// هل الدورة نشطة؟
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// تاريخ بداية الدورة
    /// </summary>
    public DateOnly? StartDate { get; set; }
    
    /// <summary>
    /// تاريخ نهاية الدورة
    /// </summary>
    public DateOnly? EndDate { get; set; }
    
    public int TeacherId { get; set; }
    
    /// <summary>
    /// المجال التعليمي
    /// </summary>
    public int DomainId { get; set; }
    
    // المحتوى التعليمي
    public int SubjectId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    
    /// <summary>
    /// طريقة التدريس (حضوري/عن بعد)
    /// </summary>
    public int TeachingModeId { get; set; }
    
    /// <summary>
    /// نوع الجلسة (فردي/جماعي)
    /// </summary>
    public int SessionTypeId { get; set; }
    
    /// <summary>
    /// هل الدورة مرنة (بدون عدد جلسات محدد)؟
    /// </summary>
    public bool IsFlexible { get; set; }
    
    /// <summary>
    /// عدد الجلسات (للدورات غير المرنة)
    /// </summary>
    public int? SessionsCount { get; set; }
    
    /// <summary>
    /// مدة الجلسة بالدقائق
    /// </summary>
    public int? SessionDurationMinutes { get; set; }
    
    /// <summary>
    /// السعر
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// الحد الأقصى للطلاب (للجلسات الجماعية)
    /// </summary>
    public int? MaxStudents { get; set; }
    
    /// <summary>
    /// هل يمكن تضمينها في الباقات؟
    /// </summary>
    public bool CanIncludeInPackages { get; set; } = false;
    
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    
    // Navigation Properties
    public Teacher.Teacher Teacher { get; set; } = null!;
    public EducationDomain Domain { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Curriculum? Curriculum { get; set; }
    public EducationLevel? Level { get; set; }
    public Grade? Grade { get; set; }
    public TeachingMode TeachingMode { get; set; } = null!;
    public SessionType SessionType { get; set; } = null!;
    
    public ICollection<CourseSession> CourseSessions { get; set; } = new List<CourseSession>();
    public ICollection<CourseEnrollmentRequest> CourseEnrollmentRequests { get; set; } = new List<CourseEnrollmentRequest>();
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
    
    /// <summary>
    /// المقاعد المتاحة (للجلسات الجماعية) - غير مخزنة في قاعدة البيانات
    /// </summary>
    [NotMapped]
    public int AvailableSeats => MaxStudents.HasValue 
        ? MaxStudents.Value - CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
        : int.MaxValue;
}
