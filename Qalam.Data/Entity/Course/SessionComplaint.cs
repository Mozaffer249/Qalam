using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Course;

/// <summary>Student or admin-initiated complaint tied to a course schedule session.</summary>
public class SessionComplaint : AuditableEntity
{
    public int Id { get; set; }
    public int CourseScheduleId { get; set; }
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int TeacherId { get; set; }
    public SessionComplaintReason ReasonCode { get; set; }
    public string Description { get; set; } = "";
    public SessionComplaintStatus Status { get; set; } = SessionComplaintStatus.Open;
    public DateTime FiledAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public SessionComplaintResolution? ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool RequiresTeacherResponse { get; set; }
    public DateTime? TeacherRespondedAt { get; set; }
    public string? TeacherResponse { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? RefundId { get; set; }
    public int? ReplacementScheduleId { get; set; }

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
    public ICollection<SessionComplaintAttachment> Attachments { get; set; } = new List<SessionComplaintAttachment>();
}
