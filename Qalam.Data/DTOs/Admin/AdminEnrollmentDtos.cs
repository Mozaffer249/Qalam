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
}

public class AdminEnrollmentParticipantDto
{
    public int ParticipantId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string PaymentStatus { get; set; } = "";
    public DateTime? PaidAt { get; set; }
}

public class AdminEnrollmentSessionDto
{
    public int ScheduleId { get; set; }
    public int SessionNumber { get; set; }
    public DateOnly Date { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
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
    public bool IsInterviewProofSession { get; set; }

    public List<AdminEnrollmentParticipantDto> Participants { get; set; } = new();
    public List<AdminEnrollmentSessionDto> Sessions { get; set; } = new();
    public AdminEnrollmentFreeTrialDto? FreeTrialConsumption { get; set; }
    public int RefundCount { get; set; }
}
