using System.Text.Json;
using System.Text.Json.Serialization;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Policy;

/// <summary>
/// Admin-configurable cancellation &amp; refund rules. Stored as JSON in <c>finance.PolicyVersions.RulesJson</c>.
/// A published version is immutable; enrollments keep the version that applied when they were activated.
/// </summary>
public class CancellationPolicyRules
{
    public BeforeFirstSessionRules BeforeFirstSession { get; set; } = new();
    public AfterFirstSessionRules AfterFirstSession { get; set; } = new();
    public SessionCancellationRules SessionCancellation { get; set; } = new();
    public TeacherNoShowRules TeacherNoShow { get; set; } = new();
    public StudentNoShowRules StudentNoShow { get; set; } = new();
    public TechnicalIssueRules TechnicalIssue { get; set; } = new();
    public AdminExceptionRules AdminExceptions { get; set; } = new();
}

public class BeforeFirstSessionRules
{
    public bool Enabled { get; set; } = true;
    /// <summary>Cancellation closes this many hours before the first session starts (0 = until start).</summary>
    public int MinHoursBeforeFirstSession { get; set; }
    public decimal RefundPct { get; set; } = 100m;
    public decimal FixedFee { get; set; }
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
    public bool AutoCancelFutureSessions { get; set; } = true;
}

public class AfterFirstSessionRules
{
    public bool Enabled { get; set; }
    public RefundCalculationMethod CalculationMethod { get; set; } = RefundCalculationMethod.UnusedSessionsCount;
    /// <summary>Percentage of the remaining (unused) value that is refunded.</summary>
    public decimal RefundPct { get; set; } = 100m;
    public decimal FixedFee { get; set; }
    public bool ExcludeCompletedSessions { get; set; } = true;
    public bool CountStudentNoShowAsUsed { get; set; } = true;
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
}

public class SessionCancellationRules
{
    public bool Enabled { get; set; } = true;
    public int NoticeHours { get; set; } = 12;
    public InWindowSessionOutcome InWindowOutcome { get; set; } = InWindowSessionOutcome.StudentChoice;
    public LateSessionOutcome LateOutcome { get; set; } = LateSessionOutcome.ConsideredUsed;
    public decimal LateRefundPct { get; set; }
    public decimal FixedFee { get; set; }
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
}

public class TeacherNoShowRules
{
    public bool Enabled { get; set; } = true;
    public TeacherNoShowOutcome Outcome { get; set; } = TeacherNoShowOutcome.Refund;
    public decimal RefundPct { get; set; } = 100m;
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
    public TeacherEarningEffect TeacherEarningEffect { get; set; } = TeacherEarningEffect.Void;
    public decimal TeacherPenaltyAmount { get; set; }
}

public class StudentNoShowRules
{
    public bool Enabled { get; set; } = true;
    public bool ConsideredUsed { get; set; } = true;
    public StudentNoShowOutcome Outcome { get; set; } = StudentNoShowOutcome.NoRefund;
    public decimal RefundPct { get; set; }
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
    public TeacherEarningEffect TeacherEarningEffect { get; set; } = TeacherEarningEffect.Keep;
    public bool AllowExceptionRequest { get; set; } = true;
}

public class TechnicalIssueRules
{
    public bool Enabled { get; set; } = true;
    public TechnicalIssueOutcome Outcome { get; set; } = TechnicalIssueOutcome.Replacement;
    public decimal RefundPct { get; set; } = 100m;
    public RefundDestination Destination { get; set; } = RefundDestination.Wallet;
    public int ReportWindowHours { get; set; } = 24;
    public TeacherEarningEffect TeacherEarningEffect { get; set; } = TeacherEarningEffect.Void;
}

public class AdminExceptionRules
{
    public bool Enabled { get; set; } = true;
    public List<AdminExceptionAction> AllowedActions { get; set; } = Enum.GetValues<AdminExceptionAction>().ToList();
    public int MinReasonLength { get; set; } = 10;
    /// <summary>Amounts above this need SuperAdmin. 0 = no limit.</summary>
    public decimal MaxAmountForAdmin { get; set; } = 1000m;
}

public enum RefundCalculationMethod { UnusedSessionsCount = 1, UnusedMinutes = 2, PercentOfPaid = 3 }
public enum InWindowSessionOutcome { Refund = 1, Reschedule = 2, StudentChoice = 3 }
public enum LateSessionOutcome { ConsideredUsed = 1, PartialRefund = 2 }
public enum TeacherNoShowOutcome { Refund = 1, Replacement = 2, RefundAndReplacement = 3, StudentChoice = 4 }
public enum StudentNoShowOutcome { NoRefund = 1, PartialRefund = 2, Reschedule = 3 }
public enum TechnicalIssueOutcome { FullRefund = 1, PartialRefund = 2, Replacement = 3, Reschedule = 4 }
public enum TeacherEarningEffect { Keep = 1, Void = 2, ProRataToRefund = 3, Penalty = 4 }
public enum AdminExceptionAction
{
    FullRefund = 1,
    PartialRefund = 2,
    WalletCredit = 3,
    Replacement = 4,
    Reschedule = 5,
    TeacherEarningAdjustment = 6
}

public static class CancellationPolicyDefaults
{
    public static CancellationPolicyRules Create() => new();

    public static string ToJson(CancellationPolicyRules rules) =>
        JsonSerializer.Serialize(rules, JsonOptions);

    public static CancellationPolicyRules FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Create();
        try
        {
            return JsonSerializer.Deserialize<CancellationPolicyRules>(json, JsonOptions) ?? Create();
        }
        catch (JsonException)
        {
            return Create();
        }
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
