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

        return SpanMinutes(StartTime, EndTime);
    }

    /// <summary>An end time at or before the start time means the slot ends on the next day (e.g. 23:00–00:00).</summary>
    public bool EndsNextDay => EndsNextDayFor(StartTime, EndTime);

    /// <summary>Calendar date on which the slot ends when it starts on <paramref name="startDate"/>.</summary>
    public DateOnly GetEndDate(DateOnly startDate) => EndsNextDay ? startDate.AddDays(1) : startDate;

    public static bool EndsNextDayFor(TimeSpan start, TimeSpan end) => end <= start;

    /// <summary>Slot length in minutes; wraps past midnight. Returns 0 when start equals end.</summary>
    public static int SpanMinutes(TimeSpan start, TimeSpan end)
    {
        if (start == end)
            return 0;

        var span = end - start;
        if (span < TimeSpan.Zero)
            span += TimeSpan.FromDays(1);

        return (int)Math.Round(span.TotalMinutes, MidpointRounding.AwayFromZero);
    }
}

