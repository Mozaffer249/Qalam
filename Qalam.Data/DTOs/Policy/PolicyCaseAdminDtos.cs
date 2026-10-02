namespace Qalam.Data.DTOs.Policy;

public class PolicyCaseListItemDto
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public string Status { get; set; } = "";
    public int EnrollmentId { get; set; }
    public int? CourseScheduleId { get; set; }
    public string? StudentName { get; set; }
    public string? TeacherName { get; set; }
    public int? PolicyVersionNumber { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TeacherEarningImpact { get; set; }
    public decimal PlatformRevenueImpact { get; set; }
    public string Currency { get; set; } = "";
    public string? Destination { get; set; }
    public string ActorRole { get; set; } = "";
    public int? ActorUserId { get; set; }
    public string? Reason { get; set; }
    public int? ReversesCaseId { get; set; }
    public int? ReversedByCaseId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PolicyCaseLinkedRefundDto
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public string Currency { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PolicyCaseLinkedWalletTxDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PolicyCaseLinkedAdjustmentDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string Kind { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PolicyCaseDetailDto : PolicyCaseListItemDto
{
    public int? PaymentId { get; set; }
    public int? RefundId { get; set; }
    public int? ReplacementScheduleId { get; set; }
    public int? ComplaintId { get; set; }
    public decimal GrossValue { get; set; }
    public string? RuleSectionJson { get; set; }
    public string? InputsJson { get; set; }
    public List<PolicyExplanationDto> Explanation { get; set; } = new();
    public List<PolicyCaseLinkedRefundDto> Refunds { get; set; } = new();
    public List<PolicyCaseLinkedWalletTxDto> WalletTransactions { get; set; } = new();
    public List<PolicyCaseLinkedAdjustmentDto> TeacherAdjustments { get; set; } = new();
    /// <summary>False when the case is already reversed, is itself a reversal, or refunded to the original payment method.</summary>
    public bool CanReverse { get; set; }
    public string? CannotReverseReason { get; set; }
}

public class PolicyCaseListFilter
{
    public string? Kind { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }
    public int? EnrollmentId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PolicyCaseListResultDto
{
    public List<PolicyCaseListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminPolicyExceptionRequest
{
    public int EnrollmentId { get; set; }
    public int? ScheduleId { get; set; }
    public int? PaymentId { get; set; }
    /// <summary>FullRefund | PartialRefund | WalletCredit | Replacement | Reschedule | TeacherEarningAdjustment.</summary>
    public string Action { get; set; } = "";
    public decimal Amount { get; set; }
    /// <summary>Wallet | OriginalMethod (refund actions only; default Wallet).</summary>
    public string? Destination { get; set; }
    /// <summary>Signed: negative deducts from the teacher, positive adds to the teacher's balance.</summary>
    public decimal? TeacherAdjustment { get; set; }
    public DateOnly? NewDate { get; set; }
    public int? NewTeacherAvailabilityId { get; set; }
    public string Reason { get; set; } = "";
}

public class ReversePolicyCaseRequest
{
    public string Reason { get; set; } = "";
}

public class FinancialTimelineEntryDto
{
    public DateTime OccurredAt { get; set; }
    /// <summary>Enrollment | Payment | PolicyCase | Refund | Wallet | Earning | Adjustment | Session | Notification.</summary>
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Detail { get; set; }
    public string? ActorRole { get; set; }
    public int? ActorUserId { get; set; }
    /// <summary>Signed money impact from the student's point of view (positive = money back to the student).</summary>
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public int? PolicyVersionNumber { get; set; }
    public int? PolicyCaseId { get; set; }
    public int? CourseScheduleId { get; set; }
    public string? Status { get; set; }
    public string Key { get; set; } = "";
}
