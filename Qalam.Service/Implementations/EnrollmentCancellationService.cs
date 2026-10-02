using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Implementations;

public class EnrollmentCancellationService : IEnrollmentCancellationService
{
    private readonly ApplicationDBContext _db;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;
    private readonly IPolicyContextBuilder _contextBuilder;
    private readonly ICancellationPolicyEngine _engine;
    private readonly IPolicyCaseExecutor _executor;

    public EnrollmentCancellationService(
        ApplicationDBContext db,
        IFreeSessionPolicyService freeSessionPolicy,
        IPolicyContextBuilder contextBuilder,
        ICancellationPolicyEngine engine,
        IPolicyCaseExecutor executor)
    {
        _db = db;
        _freeSessionPolicy = freeSessionPolicy;
        _contextBuilder = contextBuilder;
        _engine = engine;
        _executor = executor;
    }

    public async Task<PolicyPreviewDto?> PreviewAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var bundle = await _contextBuilder.BuildAsync(enrollmentId, PolicyCaseKind.BeforeFirstSessionCancel, cancellationToken: cancellationToken);
        if (bundle == null)
            return null;

        return bundle.Enrollment.EnrollmentStatus switch
        {
            EnrollmentStatus.PendingPayment => new PolicyPreviewDto
            {
                Allowed = true,
                Kind = PolicyCaseKind.BeforeFirstSessionCancel.ToString(),
                PolicyVersionNumber = bundle.Policy.VersionNumber,
                Currency = bundle.Context.Currency,
                CancelsEnrollment = true,
                Explanation =
                {
                    new PolicyExplanationDto
                    {
                        Ar = "لم يتم الدفع بعد؛ سيُلغى التسجيل دون أي رسوم.",
                        En = "Nothing has been paid yet; the enrollment is cancelled at no cost."
                    }
                }
            },
            EnrollmentStatus.Active => _executor.ToPreview(bundle, _engine.Evaluate(bundle.Context, bundle.Policy.Rules)),
            _ => new PolicyPreviewDto
            {
                Allowed = false,
                DenyCode = PolicyDenyCodes.CancelDisabled,
                Kind = PolicyCaseKind.BeforeFirstSessionCancel.ToString(),
                PolicyVersionNumber = bundle.Policy.VersionNumber,
                Currency = bundle.Context.Currency,
                Explanation =
                {
                    new PolicyExplanationDto
                    {
                        Ar = "لا يمكن إلغاء هذا التسجيل في حالته الحالية.",
                        En = "This enrollment cannot be cancelled in its current status."
                    }
                }
            }
        };
    }

    public async Task<PolicyOutcomeDto?> CancelAsync(
        int enrollmentId,
        int cancelledByUserId,
        string? reason = null,
        CancellationToken cancellationToken = default,
        string actorRole = "Student")
    {
        var bundle = await _contextBuilder.BuildAsync(enrollmentId, PolicyCaseKind.BeforeFirstSessionCancel, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Enrollment not found.");
        var enrollment = bundle.Enrollment;

        if (enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment)
        {
            foreach (var schedule in enrollment.CourseSchedules
                         .Where(s => s.Status is ScheduleStatus.Scheduled or ScheduleStatus.InProgress))
            {
                schedule.Status = ScheduleStatus.Cancelled;
                schedule.CancellationReason = ScheduleCancellationReason.StudentCancel;
            }
            await FinishCancellationAsync(enrollment, cancelledByUserId, reason, fullyRefunded: false, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            throw new InvalidOperationException("Only pending-payment or active enrollments can be cancelled.");

        var decision = _engine.Evaluate(bundle.Context, bundle.Policy.Rules);
        if (!decision.Allowed)
            throw new PolicyDeniedException(decision);

        var fullyRefunded = decision.RefundAmount >= bundle.Context.TotalRefundable - 0.001m;
        var policyCase = await _executor.ApplyAsync(
            bundle,
            decision,
            new PolicyActor(cancelledByUserId, actorRole),
            new PolicyApplyOptions
            {
                Reason = reason ?? "Student cancelled enrollment",
                ScheduleReason = ScheduleCancellationReason.StudentCancel,
                BeforeCommit = _ => FinishCancellationAsync(enrollment, cancelledByUserId, reason, fullyRefunded, cancellationToken)
            },
            cancellationToken);

        return _executor.ToOutcome(bundle, decision, policyCase);
    }

    private async Task FinishCancellationAsync(
        Enrollment enrollment,
        int cancelledByUserId,
        string? reason,
        bool fullyRefunded,
        CancellationToken cancellationToken)
    {
        enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
        enrollment.CancelledAt = DateTime.UtcNow;
        enrollment.CancelledByUserId = cancelledByUserId;

        foreach (var participant in enrollment.Participants)
        {
            if (participant.PaymentStatus == PaymentStatus.Pending)
                participant.PaymentStatus = PaymentStatus.Cancelled;
            else if (participant.PaymentStatus == PaymentStatus.Succeeded && fullyRefunded)
                participant.PaymentStatus = PaymentStatus.Refunded;
        }

        if (enrollment.EnrollmentRequest != null
            && enrollment.EnrollmentRequest.Status is RequestStatus.Pending or RequestStatus.Approved)
        {
            enrollment.EnrollmentRequest.Status = RequestStatus.Cancelled;
        }

        if (enrollment.IsFreeTrial && !EnrollmentLifecycleRules.HasSessionStarted(enrollment))
        {
            await _freeSessionPolicy.CancelConsumptionBeforeStartAsync(
                enrollment.Id,
                cancelledByUserId,
                reason,
                cancellationToken);
        }

        await _freeSessionPolicy.TryRevertTeacherInterviewFromEnrollmentAsync(
            enrollment.Id,
            cancellationToken);
    }
}
