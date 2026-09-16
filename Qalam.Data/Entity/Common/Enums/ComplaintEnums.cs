namespace Qalam.Data.Entity.Common.Enums;

public enum ComplaintSubjectType
{
    Session = 1,
    Enrollment = 2,
    Payment = 3,
    Refund = 4,
    OpenSessionRequest = 5,
    Other = 6,
}

/// <summary>
/// Unified complaint lifecycle. Session APIs map legacy statuses onto this FSM.
/// </summary>
public enum ComplaintStatus
{
    Submitted = 1,
    InReview = 2,
    AwaitingComplainant = 3,
    AwaitingRespondent = 4,
    DecisionPending = 5,
    Resolved = 6,
    Rejected = 7,
    Cancelled = 8,
}

public enum ComplaintPriority
{
    Normal = 1,
    High = 2,
    Urgent = 3,
}

public enum ComplaintComplainantRole
{
    Student = 1,
    Guardian = 2,
    Teacher = 3,
    Admin = 4,
}

public enum ComplaintReason
{
    TeacherNoShow = 1,
    TeacherLate = 2,
    QualityIssue = 3,
    TechnicalIssue = 4,
    StudentNoShow = 5,
    PaymentIssue = 6,
    EnrollmentIssue = 7,
    RefundIssue = 8,
    OpenSessionRequestIssue = 9,
    Other = 10,
}

public enum ComplaintResolution
{
    NoAction = 1,
    FullRefund = 2,
    PartialRefund = 3,
    ReplacementSession = 4,
    WarnTeacher = 5,
    DeductTeacherEarning = 6,
    RejectComplaint = 7,
    CancelledByAdmin = 8,
}

public enum ComplaintTimelineEventType
{
    Filed = 1,
    StatusChanged = 2,
    Assigned = 3,
    PriorityChanged = 4,
    ComplainantResponded = 5,
    RespondentResponded = 6,
    InfoRequested = 7,
    Resolved = 8,
    Rejected = 9,
    Cancelled = 10,
    AttachmentAdded = 11,
    NoteAdded = 12,
}
