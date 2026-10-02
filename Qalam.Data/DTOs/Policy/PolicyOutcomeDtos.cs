namespace Qalam.Data.DTOs.Policy;

public class PolicyExplanationDto
{
    public string Ar { get; set; } = "";
    public string En { get; set; } = "";
}

/// <summary>What a cancellation would do, shown to the student before confirming. Never includes teacher or platform amounts.</summary>
public class PolicyPreviewDto
{
    public bool Allowed { get; set; }
    public string? DenyCode { get; set; }
    public string Kind { get; set; } = "";
    public int PolicyVersionNumber { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal GrossValue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    /// <summary>Wallet or OriginalMethod.</summary>
    public string Destination { get; set; } = "";
    public bool CancelsEnrollment { get; set; }
    public int CancelledSessionsCount { get; set; }
    public bool CreatesReplacement { get; set; }
    public bool Reschedule { get; set; }
    public bool SessionConsideredUsed { get; set; }
    public List<PolicyExplanationDto> Explanation { get; set; } = new();
}

/// <summary>Result of an applied policy case (student-facing).</summary>
public class PolicyOutcomeDto : PolicyPreviewDto
{
    public int CaseId { get; set; }
    public int? RefundId { get; set; }
    public int? ReplacementScheduleId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PolicySummarySectionDto
{
    public string Section { get; set; } = "";
    public bool Enabled { get; set; }
    public List<PolicyExplanationDto> Lines { get; set; } = new();
}

/// <summary>Plain-language policy that applies to an enrollment (or the current policy).</summary>
public class PolicySummaryDto
{
    public int PolicyVersionNumber { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public List<PolicySummarySectionDto> Sections { get; set; } = new();
}
