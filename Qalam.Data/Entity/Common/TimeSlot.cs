using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Common;

public class TimeSlot : AuditableEntity
{
    public int Id { get; set; }
    
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    
    [MaxLength(50)]
    public string? LabelAr { get; set; } // مثل: "فترة الصباح"
    
    [MaxLength(50)]
    public string? LabelEn { get; set; }
    
    public bool IsActive { get; set; } = true;
}

