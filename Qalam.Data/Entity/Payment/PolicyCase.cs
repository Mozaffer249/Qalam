using System.ComponentModel.DataAnnotations;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Immutable record of one applied cancellation/refund decision and its financial impact.
/// Never edited or deleted — corrections are new cases linked through <see cref="ReversesCaseId"/>.
/// </summary>
public class PolicyCase
{
    public int Id { get; set; }

    public PolicyCaseKind Kind { get; set; }

    public PolicyCaseStatus Status { get; set; } = PolicyCaseStatus.Applied;

    public int EnrollmentId { get; set; }

    public int? CourseScheduleId { get; set; }

    public int? PaymentId { get; set; }

    public int? RefundId { get; set; }

    public int? ReplacementScheduleId { get; set; }

    public int? ComplaintId { get; set; }

    public int? ReversesCaseId { get; set; }

    public int? ReversedByCaseId { get; set; }

    public int? PolicyVersionId { get; set; }

    /// <summary>The exact rule section values used for this decision.</summary>
    public string? RuleSectionJson { get; set; }

    /// <summary>Inputs the engine saw (paid, sessions used, hours to start…).</summary>
    public string? InputsJson { get; set; }

    /// <summary>Localized explanation lines shown in timelines.</summary>
    public string? ExplanationJson { get; set; }

    public decimal GrossValue { get; set; }

    public decimal RefundAmount { get; set; }

    public decimal FeeAmount { get; set; }

    /// <summary>Signed change to teacher earnings (negative = reduced).</summary>
    public decimal TeacherEarningImpact { get; set; }

    /// <summary>Signed change to platform revenue (negative = platform absorbs).</summary>
    public decimal PlatformRevenueImpact { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public RefundDestination? Destination { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>Null when applied by the system (lifecycle job).</summary>
    public int? ActorUserId { get; set; }

    [MaxLength(30)]
    public string ActorRole { get; set; } = "System";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PolicyVersion? PolicyVersion { get; set; }
    public Course.Enrollment Enrollment { get; set; } = null!;
}
