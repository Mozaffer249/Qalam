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
    public string? TeacherImageUrl { get; set; }
    public string? StudentDisplayName { get; set; }
    public string? StudentAvatarUrl { get; set; }
    public DateOnly? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public ScheduleStatus Status { get; set; }
    public bool CanJoin { get; set; }
    /// <summary>Viewing student's attendance (Pending object when participant has no row yet).</summary>
    public SessionAttendanceInfoDto? Attendance { get; set; }
    /// <summary>Session-wide teacher attendance.</summary>
    public SessionAttendanceInfoDto? TeacherAttendance { get; set; }
    /// <summary>True when session completed and viewing student has not submitted a teacher review.</summary>
    public bool CanReview { get; set; }
    public string? ReferenceCode { get; set; }
    public string? RecordingUrl { get; set; }
    /// <summary>When the session actually started (or teacher joined), if known.</summary>
    public DateTime? StartedAt { get; set; }
    /// <summary>LiveKit server URL when online and joinable/live; token via LiveToken.</summary>
    public string? MeetingUrl { get; set; }
    public List<EnrollmentSessionContentUnitDto> Units { get; set; } = new();
    public List<StudentSessionAttachmentDto> Attachments { get; set; } = new();
    public List<SessionReviewDto> Reviews { get; set; } = new();
    /// <summary>All enrollment participants with effective attendance for this session.</summary>
    public List<StudentSessionParticipantAttendanceDto> Participants { get; set; } = new();
}

/// <summary>One participant row on student session detail attendance overview.</summary>
public class StudentSessionParticipantAttendanceDto
{
    public int StudentId { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public SessionAttendanceInfoDto Attendance { get; set; } = new();
    public bool IsViewer { get; set; }
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

/// <summary>Join-based attendance snapshot for student session detail.</summary>
public class SessionAttendanceInfoDto
{
    public string Status { get; set; } = "Pending";
    public int? LateMinutes { get; set; }
    public DateTime? JoinedAt { get; set; }
    public bool IsAutoResolved { get; set; }
}
