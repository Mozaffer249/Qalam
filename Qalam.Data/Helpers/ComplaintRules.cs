using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Helpers;

public static class ComplaintRules
{
    private static readonly HashSet<(ComplaintStatus From, ComplaintStatus To)> AllowedTransitions =
    [
        (ComplaintStatus.Submitted, ComplaintStatus.InReview),
        (ComplaintStatus.Submitted, ComplaintStatus.Cancelled),
        (ComplaintStatus.InReview, ComplaintStatus.AwaitingComplainant),
        (ComplaintStatus.InReview, ComplaintStatus.AwaitingRespondent),
        (ComplaintStatus.InReview, ComplaintStatus.DecisionPending),
        (ComplaintStatus.InReview, ComplaintStatus.Cancelled),
        (ComplaintStatus.AwaitingComplainant, ComplaintStatus.InReview),
        (ComplaintStatus.AwaitingComplainant, ComplaintStatus.Cancelled),
        (ComplaintStatus.AwaitingRespondent, ComplaintStatus.InReview),
        (ComplaintStatus.AwaitingRespondent, ComplaintStatus.Cancelled),
        (ComplaintStatus.DecisionPending, ComplaintStatus.Resolved),
        (ComplaintStatus.DecisionPending, ComplaintStatus.Rejected),
        (ComplaintStatus.DecisionPending, ComplaintStatus.InReview),
        (ComplaintStatus.DecisionPending, ComplaintStatus.Cancelled),
        // Direct resolve from review (admin shortcut after enough info)
        (ComplaintStatus.InReview, ComplaintStatus.Resolved),
        (ComplaintStatus.InReview, ComplaintStatus.Rejected),
        (ComplaintStatus.AwaitingComplainant, ComplaintStatus.DecisionPending),
        (ComplaintStatus.AwaitingRespondent, ComplaintStatus.DecisionPending),
    ];

    public static bool IsOpen(ComplaintStatus status) =>
        status is ComplaintStatus.Submitted
            or ComplaintStatus.InReview
            or ComplaintStatus.AwaitingComplainant
            or ComplaintStatus.AwaitingRespondent
            or ComplaintStatus.DecisionPending;

    public static bool IsBlockingForSessionEarnings(ComplaintStatus status) => IsOpen(status);

    public static bool CanTransition(ComplaintStatus from, ComplaintStatus to) =>
        AllowedTransitions.Contains((from, to));

    public static void EnsureTransition(ComplaintStatus from, ComplaintStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Cannot transition complaint from {from} to {to}.");
    }

    public static bool SubjectMatchesLinks(
        ComplaintSubjectType type,
        int? courseScheduleId,
        int? enrollmentId,
        int? paymentId,
        int? refundId,
        int? openSessionRequestId) =>
        type switch
        {
            ComplaintSubjectType.Session => courseScheduleId is > 0 && enrollmentId is > 0,
            ComplaintSubjectType.Enrollment => enrollmentId is > 0,
            ComplaintSubjectType.Payment => paymentId is > 0,
            ComplaintSubjectType.Refund => refundId is > 0,
            ComplaintSubjectType.OpenSessionRequest => openSessionRequestId is > 0,
            ComplaintSubjectType.Other => true,
            _ => false,
        };

    public static ComplaintPriority SuggestPriority(
        ComplaintSubjectType subjectType,
        decimal? relatedAmount,
        DateTime? sessionStartsAtUtc,
        int priorOpenComplaintsForParties)
    {
        if (priorOpenComplaintsForParties >= 3)
            return ComplaintPriority.Urgent;

        if (relatedAmount.GetValueOrDefault() >= 500m)
            return ComplaintPriority.High;

        if (sessionStartsAtUtc.HasValue)
        {
            var hours = (sessionStartsAtUtc.Value - DateTime.UtcNow).TotalHours;
            if (hours is >= 0 and <= 24)
                return ComplaintPriority.Urgent;
            if (hours is >= 0 and <= 72)
                return ComplaintPriority.High;
        }

        return subjectType switch
        {
            ComplaintSubjectType.Payment or ComplaintSubjectType.Refund => ComplaintPriority.High,
            ComplaintSubjectType.Session => ComplaintPriority.Normal,
            _ => ComplaintPriority.Normal,
        };
    }

    /// <summary>Maps unified status to legacy SessionComplaintStatus string for session-scoped DTOs.</summary>
    public static SessionComplaintStatus ToLegacySessionStatus(ComplaintStatus status) =>
        status switch
        {
            ComplaintStatus.Submitted => SessionComplaintStatus.Open,
            ComplaintStatus.InReview => SessionComplaintStatus.InReview,
            ComplaintStatus.AwaitingRespondent => SessionComplaintStatus.AwaitingTeacher,
            ComplaintStatus.AwaitingComplainant => SessionComplaintStatus.AwaitingStudent,
            ComplaintStatus.DecisionPending => SessionComplaintStatus.InReview,
            ComplaintStatus.Resolved => SessionComplaintStatus.Resolved,
            ComplaintStatus.Rejected => SessionComplaintStatus.Rejected,
            ComplaintStatus.Cancelled => SessionComplaintStatus.Rejected,
            _ => SessionComplaintStatus.Open,
        };

    public static ComplaintStatus FromLegacySessionStatus(SessionComplaintStatus status) =>
        status switch
        {
            SessionComplaintStatus.Open => ComplaintStatus.Submitted,
            SessionComplaintStatus.InReview => ComplaintStatus.InReview,
            SessionComplaintStatus.AwaitingTeacher => ComplaintStatus.AwaitingRespondent,
            SessionComplaintStatus.AwaitingStudent => ComplaintStatus.AwaitingComplainant,
            SessionComplaintStatus.Resolved => ComplaintStatus.Resolved,
            SessionComplaintStatus.Rejected => ComplaintStatus.Rejected,
            _ => ComplaintStatus.Submitted,
        };

    public static ComplaintReason FromLegacySessionReason(SessionComplaintReason reason) =>
        reason switch
        {
            SessionComplaintReason.TeacherNoShow => ComplaintReason.TeacherNoShow,
            SessionComplaintReason.TeacherLate => ComplaintReason.TeacherLate,
            SessionComplaintReason.QualityIssue => ComplaintReason.QualityIssue,
            SessionComplaintReason.TechnicalIssue => ComplaintReason.TechnicalIssue,
            SessionComplaintReason.StudentNoShow => ComplaintReason.StudentNoShow,
            _ => ComplaintReason.Other,
        };

    public static SessionComplaintReason ToLegacySessionReason(ComplaintReason reason) =>
        reason switch
        {
            ComplaintReason.TeacherNoShow => SessionComplaintReason.TeacherNoShow,
            ComplaintReason.TeacherLate => SessionComplaintReason.TeacherLate,
            ComplaintReason.QualityIssue => SessionComplaintReason.QualityIssue,
            ComplaintReason.TechnicalIssue => SessionComplaintReason.TechnicalIssue,
            ComplaintReason.StudentNoShow => SessionComplaintReason.StudentNoShow,
            _ => SessionComplaintReason.Other,
        };

    public static ComplaintResolution FromLegacySessionResolution(SessionComplaintResolution resolution) =>
        (ComplaintResolution)(int)resolution;

    public static SessionComplaintResolution ToLegacySessionResolution(ComplaintResolution resolution) =>
        resolution switch
        {
            ComplaintResolution.CancelledByAdmin => SessionComplaintResolution.RejectComplaint,
            _ => (SessionComplaintResolution)(int)resolution,
        };

    public static bool CanStudentFileSessionComplaint(
        ScheduleStatus scheduleStatus,
        SessionAttendanceStatus? teacherStatus) =>
        SessionComplaintRules.CanStudentFileComplaint(scheduleStatus, teacherStatus);
}
