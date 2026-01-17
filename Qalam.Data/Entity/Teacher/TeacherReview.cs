using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// تقييمات المعلم من الطلاب
/// </summary>
public class TeacherReview : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    
    /// <summary>
    /// معرف الطالب (External: Students)
    /// </summary>
    public int StudentId { get; set; }
    
    /// <summary>
    /// معرف الجلسة (اختياري)
    /// </summary>
    public int? SessionId { get; set; }
    
    /// <summary>
    /// التقييم (1-5)
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [MaxLength(600)]
    public string? Feedback { get; set; }
    
    public bool IsApproved { get; set; } = false;
    
    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public Student.Student? Student { get; set; }
}
