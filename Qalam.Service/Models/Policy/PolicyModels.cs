using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Models.Policy;

public enum PolicySessionUsage
{
    Upcoming = 1,
    Completed = 2,
    StudentNoShow = 3,
    TeacherNoShow = 4,
    Cancelled = 5,
    InProgress = 6
}

/// <summary>What the student asked for when the rule lets them choose.</summary>
public enum PolicyStudentChoice
{
    None = 0,
    Refund = 1,
    Reschedule = 2
}

public class PolicyPaymentInfo
{
    public int PaymentId { get; set; }
    public decimal Paid { get; set; }
    public decimal AlreadyRefunded { get; set; }
    public decimal Refundable => Math.Max(0, Paid - AlreadyRefunded);
}

public class PolicySessionInfo
{
    public int ScheduleId { get; set; }
    public int DurationMinutes { get; set; }
    public PolicySessionUsage Usage { get; set; }
    public DateTime? StartUtc { get; set; }
    public bool IsReplacement { get; set; }
}

/// <summary>Everything the engine needs; built from the database by the context builder.</summary>
public class PolicyContext
{
    /// <summary>Scenario. Enrollment cancellation passes BeforeFirstSessionCancel; the engine switches to After when started.</summary>
    public PolicyCaseKind Kind { get; set; }
    public DateTime NowUtc { get; set; } = DateTime.UtcNow;
    public string Currency { get; set; } = "SAR";
    public bool IsFreeTrial { get; set; }
    public bool IsGroup { get; set; }
    public bool HasStarted { get; set; }
    public DateTime? FirstSessionStartUtc { get; set; }

    public List<PolicyPaymentInfo> Payments { get; set; } = new();
    public List<PolicySessionInfo> Sessions { get; set; } = new();

    /// <summary>Package size (sessions) used for per-session value.</summary>
    public int PackageSessionCount { get; set; }
    /// <summary>Package minutes used for per-session value.</summary>
    public int PackageMinutes { get; set; }

    /// <summary>Teacher share of the price (0..1) from the pricing snapshot, used for impact estimates.</summary>
    public decimal TeacherShareRatio { get; set; }

    public int? TargetScheduleId { get; set; }
    public PolicyStudentChoice Choice { get; set; }

    public decimal TotalPaid => Payments.Sum(p => p.Paid);
    public decimal TotalRefundable => Payments.Sum(p => p.Refundable);
    public PolicySessionInfo? Target => TargetScheduleId is int id ? Sessions.FirstOrDefault(s => s.ScheduleId == id) : null;
}

public class PolicyRefundAllocation
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
}

public class PolicyExplanationLine
{
    public string Ar { get; set; } = "";
    public string En { get; set; } = "";
}

public class PolicyDecision
{
    public PolicyCaseKind Kind { get; set; }
    public bool Allowed { get; set; }
    public string? DenyCode { get; set; }

    public decimal GrossValue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
    public List<PolicyRefundAllocation> Allocations { get; set; } = new();

    public bool CancelEnrollment { get; set; }
    public List<int> CancelScheduleIds { get; set; } = new();
    public bool CreateReplacement { get; set; }
    /// <summary>Student moves the target session to a new slot.</summary>
    public bool Reschedule { get; set; }
    public bool SessionConsideredUsed { get; set; }

    public TeacherEarningEffect TeacherEarningEffect { get; set; } = TeacherEarningEffect.Keep;
    public decimal TeacherPenaltyAmount { get; set; }

    /// <summary>Signed estimate: negative when the teacher earns less because of this case.</summary>
    public decimal TeacherEarningImpact { get; set; }
    /// <summary>Signed estimate: negative when the platform absorbs part of the refund.</summary>
    public decimal PlatformRevenueImpact { get; set; }

    public string Currency { get; set; } = "SAR";
    public object? RuleSection { get; set; }
    public List<PolicyExplanationLine> Explanation { get; set; } = new();

    public static PolicyDecision Deny(PolicyCaseKind kind, string code, string ar, string en) => new()
    {
        Kind = kind,
        Allowed = false,
        DenyCode = code,
        Explanation = { new PolicyExplanationLine { Ar = ar, En = en } }
    };
}

/// <summary>The policy does not allow the requested action; <see cref="Exception.Message"/> is the English explanation.</summary>
public class PolicyDeniedException : InvalidOperationException
{
    public string Code { get; }
    public PolicyExplanationLine? Explanation { get; }

    public PolicyDeniedException(PolicyDecision decision)
        : base(decision.Explanation.FirstOrDefault()?.En ?? decision.DenyCode ?? "Not allowed by the cancellation policy.")
    {
        Code = decision.DenyCode ?? PolicyDenyCodes.RuleDisabled;
        Explanation = decision.Explanation.FirstOrDefault();
    }
}

public static class PolicyDenyCodes
{
    public const string CancelDisabled = "POLICY_CANCEL_DISABLED";
    public const string CancelWindowClosed = "POLICY_CANCEL_WINDOW_CLOSED";
    public const string CancelAfterStartDisabled = "POLICY_CANCEL_AFTER_START_DISABLED";
    public const string SessionCancelDisabled = "POLICY_SESSION_CANCEL_DISABLED";
    public const string SessionNotUpcoming = "POLICY_SESSION_NOT_UPCOMING";
    public const string GroupSessionCancel = "POLICY_GROUP_SESSION_CANCEL_NOT_SUPPORTED";
    public const string RescheduleWindowClosed = "POLICY_RESCHEDULE_WINDOW_CLOSED";
    public const string RuleDisabled = "POLICY_RULE_DISABLED";
    public const string ExceptionsDisabled = "POLICY_EXCEPTIONS_DISABLED";
    public const string ExceptionActionNotAllowed = "POLICY_EXCEPTION_ACTION_NOT_ALLOWED";
    public const string ExceptionReasonTooShort = "POLICY_EXCEPTION_REASON_TOO_SHORT";
    public const string ExceptionAmountNeedsSuperAdmin = "POLICY_EXCEPTION_AMOUNT_NEEDS_SUPERADMIN";
    public const string NotFound = "POLICY_TARGET_NOT_FOUND";
}
