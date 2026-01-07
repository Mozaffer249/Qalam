using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teaching;

public class EducationRule : AuditableEntity
{
    public int Id { get; set; }
    
    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;
    
    // قواعد الجلسات
    public int MinSessions { get; set; } = 1;
    public int MaxSessions { get; set; } = 100;
    public int DefaultSessionDurationMinutes { get; set; } = 60;
    
    // المرونة
    public bool AllowExtension { get; set; } = true;
    public bool AllowFlexibleCourses { get; set; } = true;
    
    // الجلسات الجماعية
    public int? MaxGroupSize { get; set; }
    public int? MinGroupSize { get; set; }
    
    // ملاحظات
    [MaxLength(500)]
    public string? NotesAr { get; set; }
    
    [MaxLength(500)]
    public string? NotesEn { get; set; }
}

