namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// حالة الطلب
/// </summary>
public enum RequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// حالة الجدول
/// </summary>
public enum ScheduleStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    Rescheduled = 4,
    InProgress = 5,
}

/// <summary>
/// حضور الطالب في جلسة مجدولة.
/// </summary>
public enum SessionAttendanceStatus
{
    Pending = 0,
    Present = 1,
    Late = 2,
    Absent = 3,
    Excused = 4,
}

/// <summary>Who triggered a live-room presence event.</summary>
public enum LivePresenceRole
{
    Teacher = 1,
    Student = 2,
}

/// <summary>LiveKit room presence event kind.</summary>
public enum LivePresenceEventType
{
    Joined = 1,
    Left = 2,
}

public enum SessionComplaintReason
{
    TeacherNoShow = 1,
    TeacherLate = 2,
    QualityIssue = 3,
    TechnicalIssue = 4,
    StudentNoShow = 5,
    Other = 6,
}

public enum SessionComplaintStatus
{
    Open = 1,
    InReview = 2,
    AwaitingTeacher = 3,
    AwaitingStudent = 4,
    Resolved = 5,
    Rejected = 6,
}

public enum SessionComplaintResolution
{
    NoAction = 1,
    FullRefund = 2,
    PartialRefund = 3,
    ReplacementSession = 4,
    WarnTeacher = 5,
    DeductTeacherEarning = 6,
    RejectComplaint = 7,
}

public enum SessionAuditActionType
{
    ComplaintFiled = 1,
    ComplaintStatusChanged = 2,
    AttendanceSet = 3,
    SessionCancelled = 4,
    SessionRescheduled = 5,
    RefundIssued = 6,
    EarningHeld = 7,
    EarningReleased = 8,
    EarningVoided = 9,
    TeacherWarned = 10,
    TeacherBlocked = 11,
    ReplacementSessionGranted = 12,
}
