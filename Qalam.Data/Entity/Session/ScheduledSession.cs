using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Data.Entity.Session;

/// <summary>
/// جلسة مجدولة في تاريخ ووقت محدد
/// </summary>
public class ScheduledSession : AuditableEntity
{
    public int Id { get; set; }
    
    public int SessionId { get; set; }
    
    /// <summary>
    /// تاريخ الجلسة
    /// </summary>
    public DateOnly Date { get; set; }
    
    public int TimeSlotId { get; set; }
    
    /// <summary>
    /// طريقة التدريس
    /// </summary>
    public int TeachingModeId { get; set; }
    
    /// <summary>
    /// الموقع (للتدريس الحضوري)
    /// </summary>
    public int? LocationId { get; set; }
    
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    
    // Navigation Properties
    public Session Session { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
    public TeachingMode TeachingMode { get; set; } = null!;
    public Location? Location { get; set; }
}
