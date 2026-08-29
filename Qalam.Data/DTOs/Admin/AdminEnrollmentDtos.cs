using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class AdminEnrollmentListFilter
{
    public EnrollmentStatus? Status { get; set; }
    public EnrollmentSource? Source { get; set; }
    public EnrollmentKind? Kind { get; set; }
    public bool? IsFreeTrial { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public int? CourseId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class AdminEnrollmentListItemDto
{
    public int Id { get; set; }
    public string EnrollmentStatus { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Source { get; set; } = "";
    public bool IsFreeTrial { get; set; }
    public int? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string? SubjectNameEn { get; set; }
    public string? SubjectNameAr { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int? PrimaryStudentId { get; set; }
    public string? PrimaryStudentName { get; set; }
    public int ParticipantCount { get; set; }
    public decimal GrossPackageTotal { get; set; }
    public decimal FreeSessionCredit { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal PlatformCostAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public int SessionsCompleted { get; set; }
    public int SessionsTotal { get; set; }

    public bool IsInterviewPendingAtQuote { get; set; }
    public decimal ProjectedTeacherSharePct { get; set; }
    public decimal ProjectedTeacherEarningsDue { get; set; }
    public decimal ProjectedFreeSessionTeacherDeduction { get; set; }
    public decimal ProjectedPerSessionTeacherValue { get; set; }
}

public class AdminEnrollmentParticipantDto
{
    public int ParticipantId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string PaymentStatus { get; set; } = "";
    public DateTime? PaidAt { get; set; }
    public decimal Share { get; set; }
}

public class AdminEnrollmentPaymentDto
{
    public int PaymentId { get; set; }
    public string Provider { get; set; } = "";
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = "";
}

public class AdminEnrollmentSessionDto
{
    public int ScheduleId { get; set; }
    public int SessionNumber { get; set; }
    public DateOnly Date { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public bool IsFreeSession { get; set; }
    public string? Title { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal? AccruedAmount { get; set; }
    public string? EarningLineKey { get; set; }
    public bool HasOpenComplaint { get; set; }
    public string? OpenComplaintStatus { get; set; }
    public int ComplaintCount { get; set; }
    public string? EarningLineStatus { get; set; }
}

public class AdminEnrollmentEarningLineDto
{
    public int LineId { get; set; }
    public string TransactionKey { get; set; } = "";
    public int? CourseScheduleId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string EarningUiStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class AdminEnrollmentFreeTrialDto
{
    public int ConsumptionId { get; set; }
    public int StudentId { get; set; }
    public string Status { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime ReservedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool RestoredEligibility { get; set; }
}

public class AdminEnrollmentDetailDto : AdminEnrollmentListItemDto
{
    public int? EnrollmentRequestId { get; set; }
    public int? SessionRequestId { get; set; }
    public int? SessionOfferId { get; set; }
    public int? OwnerUserId { get; set; }
    public int? PaidByUserId { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancelledByLabel { get; set; }

    public decimal SnapshotTotalPrice { get; set; }
    public decimal SnapshotTeacherSharePct { get; set; }
    public decimal SnapshotTeacherEarnings { get; set; }
    public decimal SnapshotPlatformShare { get; set; }
    public int SnapshotTotalMinutes { get; set; }
    public decimal SnapshotPricePerHour { get; set; }
    public decimal? SnapshotEarningsPricePerHour { get; set; }
    public string? SnapshotMarketCode { get; set; }
    public string? SnapshotSessionTypeCode { get; set; }
    public bool IsInterviewProofSession { get; set; }

    public string? PaymentMethod { get; set; }
    public decimal AmountRemaining { get; set; }
    public int FreeSessionsCount { get; set; }
    public int PaidSessionsCount { get; set; }

    public decimal AccruedNet { get; set; }
    public decimal PackageTeacherDue { get; set; }
    public decimal RemainingToAccrue { get; set; }
    public string EnrollmentEarningUiStatus { get; set; } = "";

    public List<AdminEnrollmentPaymentDto> Payments { get; set; } = new();
    public List<AdminEnrollmentParticipantDto> Participants { get; set; } = new();
    public List<AdminEnrollmentSessionDto> Sessions { get; set; } = new();
    public List<AdminEnrollmentEarningLineDto> EarningLines { get; set; } = new();
    public AdminEnrollmentFreeTrialDto? FreeTrialConsumption { get; set; }
    public int RefundCount { get; set; }
}
