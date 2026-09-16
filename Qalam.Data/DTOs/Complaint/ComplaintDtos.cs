using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Complaint;

public class ComplaintListItemDto
{
    public int ComplaintId { get; set; }
    public string SubjectType { get; set; } = "";
    public string SubjectLabel { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public string DescriptionPreview { get; set; } = "";
    public DateTime FiledAt { get; set; }
    public string? ComplainantName { get; set; }
    public string ComplainantRole { get; set; } = "";
    public string? RespondentName { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? CourseScheduleId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? PaymentId { get; set; }
    public int? RefundId { get; set; }
    public int? OpenSessionRequestId { get; set; }
    public bool RequiresAction { get; set; }
}

public class ComplaintListFilter
{
    public ComplaintStatus? Status { get; set; }
    public ComplaintSubjectType? SubjectType { get; set; }
    public ComplaintPriority? Priority { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? ComplainantUserId { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? Scope { get; set; }
}

public class ComplaintAttachmentDto
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string? ContentType { get; set; }
}

public class ComplaintTimelineDto
{
    public int EntryId { get; set; }
    public string EventType { get; set; } = "";
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public int ActorUserId { get; set; }
    public string ActorRole { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class ComplaintDetailDto : ComplaintListItemDto
{
    public string Description { get; set; } = "";
    public int ComplainantUserId { get; set; }
    public int? AffectedStudentId { get; set; }
    public int? RespondentTeacherId { get; set; }
    public int? RespondentUserId { get; set; }
    public bool RequiresRespondentResponse { get; set; }
    public bool RequiresComplainantResponse { get; set; }
    public string? RespondentResponse { get; set; }
    public DateTime? RespondentRespondedAt { get; set; }
    public string? ComplainantResponse { get; set; }
    public DateTime? ComplainantRespondedAt { get; set; }
    public string? ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? LinkedRefundId { get; set; }
    public int? ReplacementScheduleId { get; set; }
    public List<string> AvailableActions { get; set; } = new();
    public List<ComplaintAttachmentDto> Attachments { get; set; } = new();
    public List<ComplaintTimelineDto> Timeline { get; set; } = new();
}

public class FileComplaintRequest
{
    public ComplaintSubjectType SubjectType { get; set; }
    public int? CourseScheduleId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? PaymentId { get; set; }
    public int? RefundId { get; set; }
    public int? OpenSessionRequestId { get; set; }
    public int? AffectedStudentId { get; set; }
    public ComplaintReason ReasonCode { get; set; }
    public string Description { get; set; } = "";
}

public class ResolveComplaintRequest
{
    public ComplaintResolution ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? RefundAmount { get; set; }
    public int? PaymentId { get; set; }
}

public class ComplaintResolvePreviewDto
{
    public string ResolutionCode { get; set; } = "";
    public decimal? SuggestedRefundAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public int? PaymentId { get; set; }
    public decimal RemainingRefundable { get; set; }
    public decimal? SessionEarningAmount { get; set; }
    public string? CurrentEarningStatus { get; set; }
    public string PayoutImpact { get; set; } = "None";
    public decimal? PlatformBearEstimate { get; set; }
    public string SessionEarningEffect { get; set; } = "None";
    public List<string> Warnings { get; set; } = new();
}

public class ComplaintCountsDto
{
    public int Submitted { get; set; }
    public int InReview { get; set; }
    public int AwaitingComplainant { get; set; }
    public int AwaitingRespondent { get; set; }
    public int DecisionPending { get; set; }
    public int OpenTotal { get; set; }
}
