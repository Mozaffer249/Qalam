namespace Qalam.Data.DTOs.Teaching;

public class TimeSlotDto
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? LabelAr { get; set; }
    public string? LabelEn { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? LabelAr { get; set; }
    public string? LabelEn { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? LabelAr { get; set; }
    public string? LabelEn { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SetTimeSlotActiveDto
{
    public bool IsActive { get; set; }
}
