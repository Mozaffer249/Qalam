using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherInboxSummaryDto
{
    public TeacherInboxCountsDto Counts { get; set; } = new();
}

public class TeacherInboxCountsDto
{
    public int All { get; set; }
    public int Notified { get; set; }
    public int Viewed { get; set; }
    public int OfferSubmitted { get; set; }
    public int Skipped { get; set; }
}

public class TeacherMySessionListItemDto
{
    public int Id { get; set; }
    public string CourseTitle { get; set; } = null!;
    public string SourceLabel { get; set; } = null!;
    public int SessionNumber { get; set; }
    public string SessionTitle { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public string TeachingMode { get; set; } = "Online";
    public string SessionType { get; set; } = "Individual";
    public int StudentsCount { get; set; }
    public string Status { get; set; } = "Scheduled";
}

public class TeacherMySessionDetailDto : TeacherMySessionListItemDto
{
    public string? Description { get; set; }
    public List<string> UnitNames { get; set; } = new();
    public string? Notes { get; set; }
    public string? ZoomLink { get; set; }
    public List<TeacherSessionStudentDto> Students { get; set; } = new();
    public List<TeacherSessionContentLinkDto> ContentLinks { get; set; } = new();
    public List<TeacherSessionHomeworkDto> Homework { get; set; } = new();
    public List<SessionReviewDto> Feedback { get; set; } = new();
    public DateTime? EndedAt { get; set; }
    public bool CanJoin { get; set; }
    public string TeacherAttendance { get; set; } = "Pending";
    public DateTime? TeacherJoinedAt { get; set; }
    public DateTime? TeacherLeftAt { get; set; }
    public bool TeacherInRoom { get; set; }
    public List<SessionLivePresenceEventDto> LivePresenceEvents { get; set; } = new();
    public List<SessionComplaintSummaryDto> Complaints { get; set; } = new();
    public string? EarningLineStatus { get; set; }
    public string? EarningLineKey { get; set; }
}

public class SessionLivePresenceEventDto
{
    public string Role { get; set; } = "Teacher";
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventType { get; set; } = "Joined";
    public DateTime OccurredAt { get; set; }
}

public class TeacherSessionStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentAvatarUrl { get; set; }
    public string Attendance { get; set; } = "Pending";
    public DateTime? JoinedAt { get; set; }
    public decimal? Rating { get; set; }
    public string? Note { get; set; }
}

public class TeacherFinanceSummaryDto
{
    public decimal TotalEarningsAllTime { get; set; }
    public decimal EarningsThisMonth { get; set; }
    public decimal EarningsLastMonth { get; set; }
    public decimal PendingPayout { get; set; }
    public DateTime? NextPayoutDate { get; set; }
    public decimal PlatformFeesThisMonth { get; set; }
    public decimal RefundsThisMonth { get; set; }
    public int TransactionsCount { get; set; }
    public decimal OnHold { get; set; }
    public decimal Available { get; set; }
    public decimal PaidOut { get; set; }
    public decimal RefundsImpact { get; set; }
    public decimal Deductions { get; set; }
    public decimal Penalties { get; set; }
    public decimal Settlements { get; set; }
    public int WarningsCount { get; set; }
    public decimal CurrentBalance { get; set; }
}

public class TeacherFinanceTransactionDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = "Payment";
    public string Status { get; set; } = "Completed";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; } = null!;
    public string? RelatedStudentName { get; set; }
    public string? RelatedCourseTitle { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? EnrollmentId { get; set; }
    /// <summary>Pending | Available | Paid | Refunded — for earning rows.</summary>
    public string? EarningUiStatus { get; set; }
    public int? ScheduleId { get; set; }
    public string? ReasonCode { get; set; }
    public string? Source { get; set; }
    public string? RelatedTransactionKey { get; set; }
    public string? LedgerCategory { get; set; }
    public int? ComplaintId { get; set; }
}

public class TeacherFinanceSessionDetailDto
{
    public int? ScheduleId { get; set; }
    public int? SessionNumber { get; set; }
    public DateOnly? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFreeSession { get; set; }
    public string Status { get; set; } = "";
}

public class TeacherFinancePricingSnapshotDto
{
    public decimal GrossPackageTotal { get; set; }
    public decimal FreeSessionCredit { get; set; }
    public decimal AmountDue { get; set; }
    public decimal PricePerHour { get; set; }
    public decimal? EarningsPricePerHour { get; set; }
    public int TotalMinutes { get; set; }
    public decimal TeacherSharePct { get; set; }
    public decimal TeacherEarningsDue { get; set; }
    public decimal PlatformShare { get; set; }
    public bool IsInterviewPendingAtQuote { get; set; }
}

public class TeacherFinanceProjectionDto
{
    public decimal ProjectedTeacherSharePct { get; set; }
    public decimal ProjectedTeacherEarningsDue { get; set; }
    public decimal ProjectedFreeSessionTeacherDeduction { get; set; }
    public decimal ProjectedPerSessionTeacherValue { get; set; }
}

public class TeacherFinanceCalculationDto
{
    public decimal PackageEarningsUsed { get; set; }
    public int EarnableMinutes { get; set; }
    public int SessionMinutes { get; set; }
    public decimal ProratedAmount { get; set; }
}

public class TeacherFinanceEarningLineSummaryDto
{
    public int LineId { get; set; }
    public string TransactionKey { get; set; } = "";
    public int? CourseScheduleId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string EarningUiStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class TeacherFinanceSessionAccrualDto
{
    public int ScheduleId { get; set; }
    public int SessionNumber { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFreeSession { get; set; }
    public string Status { get; set; } = "";
    public decimal? AccruedAmount { get; set; }
    public string? EarningLineKey { get; set; }
    public bool IsHighlighted { get; set; }
}

public class TeacherFinanceEnrollmentEarningsDto
{
    public int EnrollmentId { get; set; }
    public string EnrollmentStatus { get; set; } = "";
    public int SessionsCompleted { get; set; }
    public int SessionsTotal { get; set; }
    public decimal AccruedNet { get; set; }
    public decimal PackageTeacherDue { get; set; }
    public decimal RemainingToAccrue { get; set; }
    public string EnrollmentEarningUiStatus { get; set; } = "";
    public List<TeacherFinanceSessionAccrualDto> Sessions { get; set; } = new();
    public List<TeacherFinanceEarningLineSummaryDto> EarningLines { get; set; } = new();
}

public class TeacherFinanceRefundDetailDto
{
    public int RefundId { get; set; }
    public int PaymentId { get; set; }
    public string Reason { get; set; } = "";
    public decimal PaymentTotalAmount { get; set; }
    public decimal PaymentRefundedTotal { get; set; }
    public int SessionsUsed { get; set; }
    public int SessionsUnused { get; set; }
    public decimal TeacherDeductionAmount { get; set; }
    public decimal PlatformBearAmount { get; set; }
    public string PayoutImpact { get; set; } = "None";
}

public class TeacherFinancePayoutLineSummaryDto
{
    public int LineId { get; set; }
    public int EnrollmentId { get; set; }
    public string? CourseTitle { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TeacherFinancePayoutDetailDto
{
    public int PayoutItemId { get; set; }
    public int BatchId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string? MockTransferRef { get; set; }
    public decimal TotalAmount { get; set; }
    public List<TeacherFinancePayoutLineSummaryDto> Lines { get; set; } = new();
}

public class TeacherFinanceTransactionDetailDto : TeacherFinanceTransactionDto
{
    public TeacherFinanceEnrollmentEarningsDto? EnrollmentEarnings { get; set; }
    public TeacherFinanceSessionDetailDto? Session { get; set; }
    public TeacherFinancePricingSnapshotDto? Pricing { get; set; }
    public TeacherFinanceProjectionDto? Projection { get; set; }
    public TeacherFinanceCalculationDto? Calculation { get; set; }
    public TeacherFinanceRefundDetailDto? Refund { get; set; }
    public TeacherFinancePayoutDetailDto? Payout { get; set; }
}

public class TeacherNotificationsPageDto
{
    public List<TeacherNotificationDto> Items { get; set; } = new();
    public TeacherNotificationCountsDto Counts { get; set; } = new();
}

public class TeacherNotificationCountsDto
{
    public int All { get; set; }
    public int Unread { get; set; }
}

public class TeacherNotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "NewQualifiedRequest";
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string BodyAr { get; set; } = null!;
    public string BodyEn { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? RequestId { get; set; }
}
