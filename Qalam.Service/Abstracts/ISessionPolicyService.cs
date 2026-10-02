using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Abstracts;

public class StudentSessionCancelRequest
{
    public PolicyStudentChoice Choice { get; init; } = PolicyStudentChoice.Refund;
    /// <summary>Required when rescheduling.</summary>
    public DateOnly? NewDate { get; init; }
    /// <summary>Required when rescheduling; must be an active slot of the enrollment's teacher.</summary>
    public int? NewTeacherAvailabilityId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>Applies the cancellation policy to single sessions (student cancel, no-shows, teacher/admin cancel, technical issues).</summary>
public interface ISessionPolicyService
{
    /// <returns>Null when the session does not exist or the user does not own the enrollment.</returns>
    Task<PolicyPreviewDto?> PreviewStudentCancelAsync(
        int scheduleId, int userId, PolicyStudentChoice choice, CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="PolicyDeniedException"/> / <see cref="InvalidOperationException"/> when not allowed.</summary>
    Task<PolicyOutcomeDto> StudentCancelAsync(
        int scheduleId, int userId, StudentSessionCancelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Teacher no-show (lifecycle) or teacher/admin cancellation. Cancels the session and compensates the student.
    /// Returns null when the rule is disabled or a case already exists; the caller then falls back to a plain cancel.
    /// </summary>
    Task<PolicyCase?> ApplyTeacherFaultAsync(
        int scheduleId,
        PolicyCaseKind kind,
        ScheduleCancellationReason reason,
        PolicyActor actor,
        CancellationToken cancellationToken = default);

    /// <summary>Records the student no-show outcome for a completed session (once per session).</summary>
    Task<PolicyCase?> ApplyStudentNoShowAsync(int scheduleId, CancellationToken cancellationToken = default);

    /// <summary>Policy suggestion for a technical-issue complaint (no side effects).</summary>
    Task<PolicyDecision?> EvaluateTechnicalIssueAsync(int scheduleId, CancellationToken cancellationToken = default);

    /// <summary>Records the technical-issue case after the complaint resolution performed the actions.</summary>
    Task<PolicyCase?> RecordTechnicalIssueAsync(
        int scheduleId,
        int complaintId,
        int adminUserId,
        decimal refundAmount,
        int? refundId,
        int? replacementScheduleId,
        string? notes,
        CancellationToken cancellationToken = default);
}
