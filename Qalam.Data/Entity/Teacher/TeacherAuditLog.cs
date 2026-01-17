using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// سجل تدقيق تغييرات المعلم
/// </summary>
public class TeacherAuditLog : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    
    [Required, MaxLength(80)]
    public string Action { get; set; } = null!;
    
    [Required, MaxLength(80)]
    public string TableName { get; set; } = null!;
    
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    
    // Navigation
    public Teacher Teacher { get; set; } = null!;
}
