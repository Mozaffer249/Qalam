using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// أوقات توفر المعلم الأسبوعية
/// </summary>
public class TeacherAvailability : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherId { get; set; }
    
    /// <summary>
    /// يوم الأسبوع (1-7)
    /// </summary>
    public int DayOfWeekId { get; set; }
    
    public int TimeSlotId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public Teacher Teacher { get; set; } = null!;
    public DayOfWeekMaster DayOfWeek { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
}
