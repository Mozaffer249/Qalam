using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Course;

/// <summary>
/// جلسات الدورة المحددة مسبقاً
/// </summary>
public class CourseSession : AuditableEntity
{
    public int Id { get; set; }
    
    public int CourseId { get; set; }
    
    /// <summary>
    /// رقم الجلسة في الدورة
    /// </summary>
    public int SessionNumber { get; set; }
    
    [Required, MaxLength(150)]
    public string Title { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// مدة الجلسة بالدقائق
    /// </summary>
    public int DurationMinutes { get; set; }
    
    // Navigation
    public Course Course { get; set; } = null!;
}
