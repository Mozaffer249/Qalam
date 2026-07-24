using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class RescheduleMySessionRequestDto
{
    public DateOnly NewDate { get; set; }
    public int TeacherAvailabilityId { get; set; }
}

public class SetSessionAttendanceItemDto
{
    public int StudentId { get; set; }
    public SessionAttendanceStatus Status { get; set; }
    public decimal? Rating { get; set; }
    public string? Note { get; set; }
}

public class SetSessionAttendanceRequestDto
{
    public List<SetSessionAttendanceItemDto> Items { get; set; } = new();
}

public class SetSessionTeacherNoteRequestDto
{
    public string Note { get; set; } = string.Empty;
}

public class TeacherEnrollmentInvoiceDto
{
    public string? InvoiceNumber { get; set; }
    public string? DownloadUrl { get; set; }
}

public class RescheduleMySessionResultDto
{
    public int OriginalScheduleId { get; set; }
    public int NewScheduleId { get; set; }
}
