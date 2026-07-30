using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Student;

/// <summary>Student (or guardian) view of a single course schedule session.</summary>
public class StudentSessionDetailDto
{
    public int ScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public int SessionNumber { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? TeacherNote { get; set; }
    public string? TeacherDisplayName { get; set; }
    public DateOnly? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public ScheduleStatus Status { get; set; }
    public bool CanJoin { get; set; }
    public string? AttendanceStatus { get; set; }
    /// <summary>When the session actually started (or teacher joined), if known.</summary>
    public DateTime? StartedAt { get; set; }
    /// <summary>LiveKit server URL when online and joinable/live; token via LiveToken.</summary>
    public string? MeetingUrl { get; set; }
    public List<EnrollmentSessionContentUnitDto> Units { get; set; } = new();
    public List<StudentSessionAttachmentDto> Attachments { get; set; } = new();
    public List<SessionReviewDto> Reviews { get; set; } = new();
}

public class StudentSessionAttachmentDto
{
    public int Id { get; set; }
    public int ContentItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TeacherContentItemKind Kind { get; set; }
    public TeacherContentFileType? FileType { get; set; }
    public string? PublicUrl { get; set; }
}

public class StudentSessionJoinDto
{
    public string Message { get; set; } = "Joined.";
    public string? MeetingUrl { get; set; }
}
