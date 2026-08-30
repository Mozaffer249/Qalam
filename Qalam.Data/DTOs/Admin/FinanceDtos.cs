using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class AdminRefundListFilter
{
    public RefundStatus? Status { get; set; }
    public int? EnrollmentId { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public string? Search { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AdminPayoutListFilter
{
    public PayoutBatchStatus? Status { get; set; }
    public int? TeacherId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AdminPendingEarningsFilter
{
    public int? TeacherId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AdminFinanceTransactionFilter
{
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public int? EnrollmentId { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AdminRevenueListFilter
{
    public string? Source { get; set; }
    public string? Search { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class FinanceTimelineEventDto
{
    public string EventType { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public string? ActorName { get; set; }
    public string? Notes { get; set; }
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
    public DateTime? ProcessedAt { get; set; }
    public string? CourseTitle { get; set; }
    public string? PayerName { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int? ScheduleId { get; set; }
    public string? SessionLabel { get; set; }
    public decimal OriginalPaymentAmount { get; set; }
    public string? InitiatedByName { get; set; }
    public string TransactionKey { get; set; } = "";
    public string? Description { get; set; }
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
    public int? SessionComplaintId { get; set; }
    public List<int> LinkedEarningLineIds { get; set; } = new();
    public List<FinanceTimelineEventDto> Timeline { get; set; } = new();
    public string? PaymentProviderRef { get; set; }
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
    public string TransactionKey { get; set; } = "";
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
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class AdminPayoutBatchDto : AdminPayoutBatchListItemDto
{
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? FailureReason { get; set; }
    public string? AdminNotes { get; set; }
    public List<AdminPayoutItemDto> Items { get; set; } = new();
    public List<FinanceTimelineEventDto> Timeline { get; set; } = new();
}

public class PayoutActionReasonDto
{
    public string? Reason { get; set; }
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
    public string TransactionKey { get; set; } = "";
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

public class CreatePayoutBatchDto
{
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class AdminFinanceSummaryDto
{
    public decimal TotalCollected { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TeacherEarningsPending { get; set; }
    public decimal TeacherEarningsPaid { get; set; }
    public decimal PlatformNet { get; set; }
    public decimal PayoutsDraft { get; set; }
    public decimal PayoutsApproved { get; set; }
    public decimal PayoutsPaid { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class AdminFinanceTransactionDto
{
    public string Key { get; set; } = "";
    public string Type { get; set; } = "";
    public string Category { get; set; } = "Financial";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Direction { get; set; } = "credit";
    public string Status { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int? EnrollmentId { get; set; }
    public string? CourseTitle { get; set; }
    public int? ScheduleId { get; set; }
    public int? ComplaintId { get; set; }
    public string? ReasonCode { get; set; }
    public string? Source { get; set; }
    public string? RelatedTransactionKey { get; set; }
    public string? Reference { get; set; }
}

public class TeacherLedgerEntryDto
{
    public string TransactionKey { get; set; } = "";
    public string Type { get; set; } = "";
    public string Category { get; set; } = "Financial";
    public string Direction { get; set; } = "Credit";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string ReasonCode { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Source { get; set; } = "";
    public string? RelatedTransactionKey { get; set; }
    public int? ComplaintId { get; set; }
    public int? EnrollmentId { get; set; }
    public int? ScheduleId { get; set; }
    public int? TeacherId { get; set; }
    public string Status { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public string? CourseTitle { get; set; }
}

public class AdminTeacherFinanceSummaryDto
{
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal Pending { get; set; }
    public decimal OnHold { get; set; }
    public decimal Available { get; set; }
    public decimal PaidOut { get; set; }
    public decimal RefundsImpact { get; set; }
    public decimal Deductions { get; set; }
    public decimal Penalties { get; set; }
    public decimal Settlements { get; set; }
    public int WarningsCount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal PlatformCommission { get; set; }
    public string Currency { get; set; } = "SAR";
}

public class AdminRevenueSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal PlatformCommission { get; set; }
    public decimal TeacherEarnings { get; set; }
    public decimal Refunds { get; set; }
    public decimal Discounts { get; set; }
    public decimal PendingRevenue { get; set; }
    public decimal FreeTrialImpact { get; set; }
    public string Currency { get; set; } = "SAR";
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public List<AdminRevenueBySourceDto> BySource { get; set; } = new();
}

public class AdminRevenueBySourceDto
{
    public string Source { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class AdminRevenueRecordDto
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public int PaymentId { get; set; }
    public int? EnrollmentId { get; set; }
    public string? CourseTitle { get; set; }
    public decimal GrossPayment { get; set; }
    public decimal PlatformCommission { get; set; }
    public decimal TeacherEarnings { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetPlatformRevenue { get; set; }
    public bool IsFreeTrial { get; set; }
    public decimal FreeTrialImpact { get; set; }
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";
    public string Currency { get; set; } = "SAR";
    public DateTime OccurredAt { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
}

public class AdminRevenueDetailDto : AdminRevenueRecordDto
{
    public List<FinanceTimelineEventDto> Timeline { get; set; } = new();
    public int? ScheduleId { get; set; }
    public string? PaymentProviderRef { get; set; }
}
