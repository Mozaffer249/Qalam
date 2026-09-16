using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Complaint;

public class Complaint : AuditableEntity
{
    public int Id { get; set; }

    public ComplaintSubjectType SubjectType { get; set; }

    public int? CourseScheduleId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? PaymentId { get; set; }
    public int? RefundId { get; set; }
    public int? OpenSessionRequestId { get; set; }

    public int ComplainantUserId { get; set; }
    public ComplaintComplainantRole ComplainantRole { get; set; }
    public int? AffectedStudentId { get; set; }
    public int? RespondentTeacherId { get; set; }
    public int? RespondentUserId { get; set; }

    public ComplaintReason ReasonCode { get; set; }
    public string Description { get; set; } = "";
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;
    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Normal;

    public DateTime FiledAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public ComplaintResolution? ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }

    public int? AssignedToUserId { get; set; }
    public bool RequiresRespondentResponse { get; set; }
    public bool RequiresComplainantResponse { get; set; }
    public string? RespondentResponse { get; set; }
    public DateTime? RespondentRespondedAt { get; set; }
    public string? ComplainantResponse { get; set; }
    public DateTime? ComplainantRespondedAt { get; set; }

    public int? LinkedRefundId { get; set; }
    public int? ReplacementScheduleId { get; set; }

    /// <summary>Legacy SessionComplaint.Id when migrated 1:1; null for new unified-only rows.</summary>
    public int? LegacySessionComplaintId { get; set; }

    public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
    public ICollection<ComplaintTimelineEntry> Timeline { get; set; } = new List<ComplaintTimelineEntry>();
}
