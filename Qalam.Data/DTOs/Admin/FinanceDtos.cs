using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class AdminRefundListFilter
{
    public RefundStatus? Status { get; set; }
    public int? EnrollmentId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class AdminRefundListItemDto
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public int EnrollmentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ProviderRefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CourseTitle { get; set; }
    public string? PayerName { get; set; }
}

public class AdminRefundDetailDto : AdminRefundListItemDto
{
    public int? InitiatedByUserId { get; set; }
    public decimal PaymentTotalAmount { get; set; }
    public decimal PaymentRefundedTotal { get; set; }
    public int SessionsUsed { get; set; }
    public int SessionsUnused { get; set; }
    public decimal TeacherDeductionAmount { get; set; }
    public decimal PlatformBearAmount { get; set; }
    /// <summary>None | VoidedPending | AlreadyPaid</summary>
    public string PayoutImpact { get; set; } = "None";
}

public class IssueAdminRefundDto
{
    public int? PaymentId { get; set; }
    public int? EnrollmentId { get; set; }
    public decimal? Amount { get; set; }
    public string Reason { get; set; } = "";
}

public class AdminPendingEarningDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int EnrollmentId { get; set; }
    public string? CourseTitle { get; set; }
    public int? CourseScheduleId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Source { get; set; } = "";
    public bool IsFreeTrialEnrollment { get; set; }
    public int FreeSessionsInEnrollment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPayoutBatchListItemDto
{
    public int Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Status { get; set; } = "";
    public string? MockTransferRef { get; set; }
    public int ItemsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class AdminPayoutEarningLineDto
{
    public int LineId { get; set; }
    public int EnrollmentId { get; set; }
    public string? CourseTitle { get; set; }
    public int? CourseScheduleId { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int FreeSessionsInEnrollment { get; set; }
    public int SessionsCompleted { get; set; }
}

public class AdminPayoutItemDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public int LinesCount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal RefundsAmount { get; set; }
    public decimal TransferrableAmount { get; set; }
    public List<AdminPayoutEarningLineDto> Lines { get; set; } = new();
}

public class AdminPayoutBatchDto : AdminPayoutBatchListItemDto
{
    public List<AdminPayoutItemDto> Items { get; set; } = new();
}

public class CreatePayoutBatchDto
{
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}
