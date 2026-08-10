namespace Qalam.Data.DTOs.Student;

/// <summary>
/// Composite child/student file detail for the profile screen
/// (attendance + upcoming sessions + documents).
/// </summary>
public class ChildFileDetailDto
{
    public int AttendanceRatePercent { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int TotalMarkedSessions { get; set; }
    public List<ChildUpcomingSessionDto> UpcomingSessions { get; set; } = [];
    public List<ChildDocumentDto> Documents { get; set; } = [];
}

public class ChildUpcomingSessionDto
{
    public int ScheduleId { get; set; }
    public int? EnrollmentId { get; set; }
    public string? TitleEn { get; set; }
    public string? TitleAr { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? TeacherName { get; set; }
    public string? SubjectNameAr { get; set; }
    public string? SubjectNameEn { get; set; }
}

/// <summary>Placeholder for future student documents (File endpoint returns empty list for now).</summary>
public class ChildDocumentDto
{
    public int Id { get; set; }
    public string? TitleAr { get; set; }
    public string? TitleEn { get; set; }
    public string? Type { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
}
