using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Abstracts;

public record PolicyActor(int? UserId, string Role)
{
    public static readonly PolicyActor System = new(null, "System");
}

public class PolicyApplyOptions
{
    public string? Reason { get; init; }
    public int? ComplaintId { get; init; }
    /// <summary>Stored on schedules this case cancels.</summary>
    public ScheduleCancellationReason ScheduleReason { get; init; } = ScheduleCancellationReason.StudentCancel;
    /// <summary>Runs inside the case transaction after refunds, before commit (status changes owned by the caller).</summary>
    public Func<PolicyCase, Task>? BeforeCommit { get; init; }
}

public interface IPolicyCaseExecutor
{
    /// <summary>
    /// Persists one immutable <see cref="PolicyCase"/> and applies the decision (refunds, schedule
    /// cancellations, teacher earning effects, replacement) in a single transaction.
    /// </summary>
    Task<PolicyCase> ApplyAsync(
        PolicyContextBundle bundle,
        PolicyDecision decision,
        PolicyActor actor,
        PolicyApplyOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Writes the case only, for actions already performed elsewhere (e.g. complaint resolution).</summary>
    Task<PolicyCase> RecordAsync(
        PolicyContextBundle bundle,
        PolicyDecision decision,
        PolicyActor actor,
        PolicyApplyOptions? options = null,
        int? refundId = null,
        int? replacementScheduleId = null,
        CancellationToken cancellationToken = default);

    PolicyPreviewDto ToPreview(PolicyContextBundle bundle, PolicyDecision decision);

    PolicyOutcomeDto ToOutcome(PolicyContextBundle bundle, PolicyDecision decision, PolicyCase policyCase);
}
