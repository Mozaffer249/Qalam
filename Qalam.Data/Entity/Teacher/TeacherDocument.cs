using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// وثائق المعلم
/// </summary>
public class TeacherDocument : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    
    public TeacherDocumentType DocumentType { get; set; }
    
    [Required, MaxLength(400)]
    public string FilePath { get; set; } = null!;
    
    /// <summary>
    /// هل تم التحقق من الوثيقة؟
    /// </summary>
    public bool IsVerified { get; set; } = false;
    
    /// <summary>
    /// معرف المدير الذي تحقق من الوثيقة
    /// </summary>
    public int? VerifiedByAdminId { get; set; }
    
    public DateTime? VerifiedAt { get; set; }
    
    // Navigation
    public Teacher Teacher { get; set; } = null!;
}
