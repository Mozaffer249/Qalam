using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Data.DTOs.Admin;

public class SessionComplaintSummaryDto
{
    public int ComplaintId { get; set; }
    public string ReasonCode { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime FiledAt { get; set; }
    public string? ResolutionCode { get; set; }
    public string Description { get; set; } = "";
    public bool RequiresTeacherResponse { get; set; }
    public string? TeacherResponse { get; set; }
    public List<AdminSessionComplaintAttachmentDto> Attachments { get; set; } = new();
}

public class AdminSessionListFilter
{
    public ScheduleStatus? Status { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public int? EnrollmentId { get; set; }
    public bool? HasComplaint { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class AdminSessionListItemDto
{
    public int ScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public int SessionNumber { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public string? CourseTitle { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? PrimaryStudentName { get; set; }
    public bool HasOpenComplaint { get; set; }
    public int ComplaintCount { get; set; }
    public decimal? AccruedAmount { get; set; }
    public string? EarningLineStatus { get; set; }
}

public class AdminSessionDetailDto
{
    public int ScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public int SessionNumber { get; set; }
    public string Status { get; set; } = "";
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string TeachingMode { get; set; } = "";
    public string SessionType { get; set; } = "";
    public string? CourseTitle { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? TeacherNote { get; set; }
    public string? MeetingUrl { get; set; }
    public string? LiveRoomName { get; set; }
    public string Currency { get; set; } = "SAR";
    public AdminSessionTeacherAttendanceDto TeacherAttendance { get; set; } = new();
    public List<AdminSessionStudentAttendanceDto> Students { get; set; } = new();
    public List<AdminSessionLiveEventDto> LivePresenceEvents { get; set; } = new();
    public List<AdminSessionReviewDto> Reviews { get; set; } = new();
    public List<AdminSessionComplaintDto> Complaints { get; set; } = new();
    public AdminSessionFinanceDto Finance { get; set; } = new();
    public List<AdminSessionAuditEntryDto> Timeline { get; set; } = new();
}

public class AdminSessionTeacherAttendanceDto
{
    public string Status { get; set; } = "";
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool InRoom { get; set; }
}

public class AdminSessionStudentAttendanceDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string Status { get; set; } = "";
    public DateTime? JoinedAt { get; set; }
    public decimal? Rating { get; set; }
    public string? Note { get; set; }
}

public class AdminSessionLiveEventDto
{
    public string Role { get; set; } = "";
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTime OccurredAt { get; set; }
}

public class AdminSessionReviewDto
{
    public int ReviewId { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int Rating { get; set; }
    public string? Feedback { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminSessionComplaintDto
{
    public int ComplaintId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string ReasonCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime FiledAt { get; set; }
    public string? ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool RequiresTeacherResponse { get; set; }
    public string? TeacherResponse { get; set; }
    public List<AdminSessionComplaintAttachmentDto> Attachments { get; set; } = new();
}

public class AdminSessionComplaintAttachmentDto
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string? ContentType { get; set; }
}

public class AdminSessionFinanceDto
{
    public decimal? AccruedAmount { get; set; }
    public string? EarningLineKey { get; set; }
    public string? EarningLineStatus { get; set; }
    public int RefundCount { get; set; }
}

public class AdminSessionAuditEntryDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = "";
    public int ActorUserId { get; set; }
    public string ActorRole { get; set; } = "";
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SessionComplaintDetailDto
{
    public int ComplaintId { get; set; }
    public int CourseScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public string ReasonCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime FiledAt { get; set; }
    public string? ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool RequiresTeacherResponse { get; set; }
    public string? TeacherResponse { get; set; }
    public List<AdminSessionComplaintAttachmentDto> Attachments { get; set; } = new();
}

public class StudentSessionListItemDto
{
    public int ScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public int SessionNumber { get; set; }
    public string? Title { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public string DisplayStatus { get; set; } = "";
    public List<string> DisplayStatusHints { get; set; } = new();
    public bool HasOpenComplaint { get; set; }
    public bool CanFileComplaint { get; set; }
}

public class FileSessionComplaintRequest
{
    public SessionComplaintReason ReasonCode { get; set; }
    public string Description { get; set; } = "";
}

public class ResolveSessionComplaintRequest
{
    public SessionComplaintResolution ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? RefundAmount { get; set; }
    public int? PaymentId { get; set; }
}

public class AdminSetSessionAttendanceRequest
{
    public SessionAttendanceStatus? TeacherStatus { get; set; }
    public List<AdminStudentAttendanceItemRequest> Students { get; set; } = new();
}

public class AdminStudentAttendanceItemRequest
{
    public int StudentId { get; set; }
    public SessionAttendanceStatus Status { get; set; }
}

public class AdminSessionRefundRequest
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
}

public class AdminSessionRescheduleRequest
{
    public DateOnly NewDate { get; set; }
    public int TeacherAvailabilityId { get; set; }
}

public class TeacherRespondComplaintRequest
{
    public string Response { get; set; } = "";
}
