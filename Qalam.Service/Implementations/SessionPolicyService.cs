using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Implementations;

public class SessionPolicyService : ISessionPolicyService
{
    private readonly ApplicationDBContext _db;
    private readonly IPolicyContextBuilder _builder;
    private readonly ICancellationPolicyEngine _engine;
    private readonly IPolicyCaseExecutor _executor;
    private readonly IPolicyCaseRepository _cases;
    private readonly ILogger<SessionPolicyService> _logger;

    public SessionPolicyService(
        ApplicationDBContext db,
        IPolicyContextBuilder builder,
        ICancellationPolicyEngine engine,
        IPolicyCaseExecutor executor,
        IPolicyCaseRepository cases,
        ILogger<SessionPolicyService> logger)
    {
        _db = db;
        _builder = builder;
        _engine = engine;
        _executor = executor;
        _cases = cases;
        _logger = logger;
    }

    public async Task<PolicyPreviewDto?> PreviewStudentCancelAsync(
        int scheduleId, int userId, PolicyStudentChoice choice, CancellationToken cancellationToken = default)
    {
        var bundle = await LoadOwnedAsync(scheduleId, userId, choice, cancellationToken);
        if (bundle == null)
            return null;
        if (bundle.Enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            return _executor.ToPreview(bundle, PolicyDecision.Deny(PolicyCaseKind.SessionCancel, PolicyDenyCodes.SessionNotUpcoming,
                "التسجيل غير نشط.", "The enrollment is not active."));
        return _executor.ToPreview(bundle, _engine.Evaluate(bundle.Context, bundle.Policy.Rules));
    }

    public async Task<PolicyOutcomeDto> StudentCancelAsync(
        int scheduleId, int userId, StudentSessionCancelRequest request, CancellationToken cancellationToken = default)
    {
        var bundle = await LoadOwnedAsync(scheduleId, userId, request.Choice, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");
        if (bundle.Enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            throw new InvalidOperationException("The enrollment is not active.");

        var decision = _engine.Evaluate(bundle.Context, bundle.Policy.Rules);
        if (!decision.Allowed)
            throw new PolicyDeniedException(decision);

        var original = bundle.Enrollment.CourseSchedules.First(s => s.Id == scheduleId);
        Data.Entity.Teacher.TeacherAvailability? slot = null;
        if (decision.Reschedule)
        {
            if (request.NewDate is not DateOnly newDate || request.NewTeacherAvailabilityId is not int availabilityId)
                throw new InvalidOperationException("Choose a new date and time to reschedule.");
            slot = await ValidateSlotAsync(bundle.Enrollment.ApprovedByTeacherId, newDate, availabilityId, cancellationToken);
        }

        var policyCase = await _executor.ApplyAsync(
            bundle,
            decision,
            new PolicyActor(userId, "Student"),
            new PolicyApplyOptions
            {
                Reason = request.Reason ?? (decision.Reschedule ? "Student rescheduled session" : "Student cancelled session"),
                ScheduleReason = ScheduleCancellationReason.StudentCancel,
                BeforeCommit = async pc =>
                {
                    if (slot == null)
                        return;
                    original.Status = ScheduleStatus.Rescheduled;
                    original.CancellationReason = ScheduleCancellationReason.StudentReschedule;
                    original.PolicyCaseId = pc.Id;
                    var replacement = new Data.Entity.Course.CourseSchedule
                    {
                        EnrollmentId = original.EnrollmentId,
                        CourseSessionId = original.CourseSessionId,
                        Date = request.NewDate!.Value,
                        TeacherAvailabilityId = slot.Id,
                        DurationMinutes = slot.TimeSlot.ResolveDurationMinutes() is > 0 and var d ? d : original.DurationMinutes,
                        TeachingModeId = original.TeachingModeId,
                        LocationId = original.LocationId,
                        Status = ScheduleStatus.Scheduled,
                        ReplacesScheduleId = original.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.CourseSchedules.Add(replacement);
                    await _db.SaveChangesAsync(cancellationToken);
                    pc.ReplacementScheduleId = replacement.Id;
                }
            },
            cancellationToken);

        return _executor.ToOutcome(bundle, decision, policyCase);
    }

    public async Task<PolicyCase?> ApplyTeacherFaultAsync(
        int scheduleId,
        PolicyCaseKind kind,
        ScheduleCancellationReason reason,
        PolicyActor actor,
        CancellationToken cancellationToken = default)
    {
        if (await _cases.ExistsForScheduleAsync(kind, scheduleId, cancellationToken))
            return null;

        var bundle = await BuildForScheduleAsync(scheduleId, kind, PolicyStudentChoice.None, cancellationToken);
        if (bundle == null || bundle.Enrollment.EnrollmentStatus is not (EnrollmentStatus.Active or EnrollmentStatus.Completed))
            return null;

        var decision = _engine.Evaluate(bundle.Context, bundle.Policy.Rules);
        if (!decision.Allowed)
        {
            _logger.LogInformation("Teacher-fault policy not applied for schedule {ScheduleId}: {Code}", scheduleId, decision.DenyCode);
            return null;
        }

        if (reason == ScheduleCancellationReason.AdminCancel)
        {
            // Admin cancellations are not the teacher's fault: no penalty, no clawback.
            decision.TeacherEarningEffect = TeacherEarningEffect.Keep;
            decision.TeacherEarningImpact += decision.TeacherPenaltyAmount;
            decision.TeacherPenaltyAmount = 0;
            decision.PlatformRevenueImpact = Math.Round(-decision.RefundAmount - decision.TeacherEarningImpact, 2);
        }

        return await _executor.ApplyAsync(
            bundle,
            decision,
            actor,
            new PolicyApplyOptions
            {
                Reason = reason switch
                {
                    ScheduleCancellationReason.TeacherNoShow => "Teacher did not attend",
                    ScheduleCancellationReason.AdminCancel => "Session cancelled by admin",
                    _ => "Session cancelled by teacher"
                },
                ScheduleReason = reason
            },
            cancellationToken);
    }

    public async Task<PolicyCase?> ApplyStudentNoShowAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        if (await _cases.ExistsForScheduleAsync(PolicyCaseKind.StudentNoShow, scheduleId, cancellationToken))
            return null;

        var bundle = await BuildForScheduleAsync(scheduleId, PolicyCaseKind.StudentNoShow, PolicyStudentChoice.None, cancellationToken);
        if (bundle == null)
            return null;

        var decision = _engine.Evaluate(bundle.Context, bundle.Policy.Rules);
        if (!decision.Allowed)
            return null;

        return await _executor.ApplyAsync(
            bundle,
            decision,
            PolicyActor.System,
            new PolicyApplyOptions { Reason = "Student did not attend" },
            cancellationToken);
    }

    public async Task<PolicyDecision?> EvaluateTechnicalIssueAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var bundle = await BuildForScheduleAsync(scheduleId, PolicyCaseKind.TechnicalIssue, PolicyStudentChoice.None, cancellationToken);
        return bundle == null ? null : _engine.Evaluate(bundle.Context, bundle.Policy.Rules);
    }

    public async Task<PolicyCase?> RecordTechnicalIssueAsync(
        int scheduleId,
        int complaintId,
        int adminUserId,
        decimal refundAmount,
        int? refundId,
        int? replacementScheduleId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var bundle = await BuildForScheduleAsync(scheduleId, PolicyCaseKind.TechnicalIssue, PolicyStudentChoice.None, cancellationToken);
        if (bundle == null)
            return null;

        var decision = _engine.Evaluate(bundle.Context, bundle.Policy.Rules);
        if (!decision.Allowed)
        {
            decision = new PolicyDecision { Kind = PolicyCaseKind.TechnicalIssue, Allowed = true, Currency = bundle.Context.Currency };
        }

        decision.RefundAmount = refundAmount;
        decision.FeeAmount = 0;
        decision.CreateReplacement = replacementScheduleId != null;
        decision.PlatformRevenueImpact = Math.Round(-refundAmount - decision.TeacherEarningImpact, 2);
        decision.Explanation.Add(new PolicyExplanationLine
        {
            Ar = "تمت معالجة المشكلة التقنية من خلال الشكوى.",
            En = "The technical issue was handled through the complaint."
        });

        return await _executor.RecordAsync(
            bundle,
            decision,
            new PolicyActor(adminUserId, "Admin"),
            new PolicyApplyOptions { Reason = notes ?? $"Technical issue complaint #{complaintId}", ComplaintId = complaintId },
            refundId,
            replacementScheduleId,
            cancellationToken);
    }

    private async Task<PolicyContextBundle?> BuildForScheduleAsync(
        int scheduleId, PolicyCaseKind kind, PolicyStudentChoice choice, CancellationToken cancellationToken)
    {
        var enrollmentId = await _builder.GetEnrollmentIdForScheduleAsync(scheduleId, cancellationToken);
        return enrollmentId == null
            ? null
            : await _builder.BuildAsync(enrollmentId.Value, kind, scheduleId, choice, cancellationToken);
    }

    private async Task<PolicyContextBundle?> LoadOwnedAsync(
        int scheduleId, int userId, PolicyStudentChoice choice, CancellationToken cancellationToken)
    {
        var bundle = await BuildForScheduleAsync(scheduleId, PolicyCaseKind.SessionCancel, choice, cancellationToken);
        if (bundle == null)
            return null;
        var owner = bundle.Enrollment.OwnerUserId ?? bundle.Enrollment.EnrollmentRequest?.RequestedByUserId;
        return owner == userId ? bundle : null;
    }

    private async Task<Data.Entity.Teacher.TeacherAvailability> ValidateSlotAsync(
        int teacherId, DateOnly date, int availabilityId, CancellationToken cancellationToken)
    {
        var slot = await _db.TeacherAvailabilities
            .Include(a => a.TimeSlot)
            .FirstOrDefaultAsync(a => a.Id == availabilityId, cancellationToken);
        if (slot == null || !slot.IsActive || slot.TeacherId != teacherId)
            throw new InvalidOperationException("The selected time is not available for this teacher.");

        if (PlatformTime.ToUtc(date, slot.TimeSlot.StartTime) <= DateTime.UtcNow)
            throw new InvalidOperationException("The new time must be in the future.");

        var taken = await _db.CourseSchedules.AnyAsync(s => s.Date == date
                                                             && s.TeacherAvailabilityId == availabilityId
                                                             && (s.Status == ScheduleStatus.Scheduled || s.Status == ScheduleStatus.InProgress),
            cancellationToken);
        if (taken)
            throw new InvalidOperationException("The selected time is already booked.");

        return slot;
    }
}
