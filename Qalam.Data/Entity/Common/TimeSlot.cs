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

    /// <summary>
    /// Minutes for billing/scheduling: uses stored <see cref="DurationMinutes"/> when positive;
    /// otherwise derives length from <see cref="StartTime"/>–<see cref="EndTime"/> (handles stale/zero DurationMinutes rows).
    /// </summary>
    public int ResolveDurationMinutes()
    {
        if (DurationMinutes > 0)
            return DurationMinutes;

        var span = EndTime - StartTime;
        if (span <= TimeSpan.Zero)
            return 0;

        return (int)Math.Round(span.TotalMinutes, MidpointRounding.AwayFromZero);
    }
}

