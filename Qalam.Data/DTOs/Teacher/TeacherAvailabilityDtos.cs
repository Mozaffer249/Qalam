using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

#region Input DTOs

/// <summary>
/// DTO لحفظ أوقات توفر المعلم (جدول أسبوعي كامل)
/// </summary>
public class SaveTeacherAvailabilityDto
{
    public List<DayAvailabilityDto> DaySchedules { get; set; } = new();
}

/// <summary>
/// DTO لأوقات التوفر في يوم واحد
/// </summary>
public class DayAvailabilityDto
{
    /// <summary>
    /// يوم الأسبوع (1-7)
    /// </summary>
    public int DayOfWeekId { get; set; }

    /// <summary>
    /// الفترات الزمنية المتاحة في هذا اليوم
    /// </summary>
    public List<int> TimeSlotIds { get; set; } = new();
}

/// <summary>
/// DTO لإضافة استثناء (إجازة أو وقت إضافي)
/// </summary>
public class AddAvailabilityExceptionDto
{
    public DateOnly Date { get; set; }
    public int TimeSlotId { get; set; }
    public AvailabilityExceptionType ExceptionType { get; set; }
    public string? Reason { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// DTO لاستجابة أوقات توفر المعلم (الجدول الأسبوعي + الاستثناءات)
/// </summary>
public class TeacherAvailabilityResponseDto
{
    public int TeacherId { get; set; }
    public List<DayScheduleResponseDto> WeeklySchedule { get; set; } = new();
    public List<AvailabilityExceptionResponseDto> Exceptions { get; set; } = new();
}

/// <summary>
/// DTO لجدول يوم واحد
/// </summary>
public class DayScheduleResponseDto
{
    public int DayOfWeekId { get; set; }
    public string DayNameAr { get; set; } = default!;
    public string DayNameEn { get; set; } = default!;
    public List<TimeSlotResponseDto> TimeSlots { get; set; } = new();
}

/// <summary>
/// DTO لفترة زمنية واحدة
/// </summary>
public class TimeSlotResponseDto
{
    public int Id { get; set; }
    public int AvailabilityId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? LabelAr { get; set; }
    public string? LabelEn { get; set; }
}

/// <summary>
/// DTO لاستثناء التوفر (إجازة أو وقت إضافي)
/// </summary>
public class AvailabilityExceptionResponseDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeSlotResponseDto TimeSlot { get; set; } = default!;
    public AvailabilityExceptionType ExceptionType { get; set; }
    public string? ExceptionTypeDisplay { get; set; }
    public string? Reason { get; set; }
}

#endregion

#region Student-Facing Date-Range View

/// <summary>Status of a single slot on a specific date.</summary>
public enum AvailabilitySlotStatus
{
    Free = 0,
    Booked = 1,   // a CourseSchedule with Status=Scheduled exists for this (Date, TeacherAvailabilityId)
    Blocked = 2   // a TeacherAvailabilityException with ExceptionType=Blocked exists for this (Date, TimeSlotId)
}

/// <summary>
/// Materialised slot for a specific date — derived from the weekly TeacherAvailability template
/// plus per-date overrides (exceptions) and existing bookings (CourseSchedules).
/// </summary>
public class AvailabilitySlotByDateDto
{
    public int TeacherAvailabilityId { get; set; }
    public int TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? LabelEn { get; set; }
    public AvailabilitySlotStatus Status { get; set; }
}

public class AvailabilityDayDto
{
    public DateOnly Date { get; set; }
    public int DayOfWeekId { get; set; }
    public string DayNameEn { get; set; } = default!;
    public List<AvailabilitySlotByDateDto> Slots { get; set; } = new();
}

/// <summary>
/// Response for "what dates / slots is this teacher actually free on?".
/// Only days that have at least one matching weekly availability are included.
/// </summary>
public class TeacherAvailabilityByRangeDto
{
    public int TeacherId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<AvailabilityDayDto> Days { get; set; } = new();
}

#endregion

#region Student-Facing Weekday-Grouped Date-Range View

public class AvailabilitySlotDateStatusDto
{
    public DateOnly Date { get; set; }
    public AvailabilitySlotStatus Status { get; set; }
}

public class AvailabilitySlotByWeekdayDto
{
    public int TeacherAvailabilityId { get; set; }
    public int TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? LabelEn { get; set; }
    public List<AvailabilitySlotDateStatusDto> Dates { get; set; } = new();
}

public class AvailabilityWeekdayDto
{
    public int DayOfWeekId { get; set; }
    public string DayNameEn { get; set; } = default!;
    public List<AvailabilitySlotByWeekdayDto> Slots { get; set; } = new();
}

public class TeacherAvailabilityByWeekdayRangeDto
{
    public int TeacherId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<AvailabilityWeekdayDto> Weekdays { get; set; } = new();
}

#endregion
