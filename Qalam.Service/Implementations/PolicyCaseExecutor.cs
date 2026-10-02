using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Implementations;

public class PolicyCaseExecutor : IPolicyCaseExecutor
{
    private readonly ApplicationDBContext _db;
    private readonly IRefundService _refunds;

    public PolicyCaseExecutor(ApplicationDBContext db, IRefundService refunds)
    {
        _db = db;
        _refunds = refunds;
    }

    public async Task<PolicyCase> ApplyAsync(
        PolicyContextBundle bundle,
        PolicyDecision decision,
        PolicyActor actor,
        PolicyApplyOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!decision.Allowed)
            throw new InvalidOperationException(decision.DenyCode ?? "POLICY_DENIED");
        options ??= new PolicyApplyOptions();

        var ctx = bundle.Context;
        var enrollment = bundle.Enrollment;

        IDbContextTransaction? tx = null;
        if (_db.Database.IsRelational() && _db.Database.CurrentTransaction == null)
            tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var policyCase = new PolicyCase
            {
                Kind = decision.Kind,
                Status = PolicyCaseStatus.Applied,
                EnrollmentId = enrollment.Id,
                CourseScheduleId = ctx.TargetScheduleId,
                ComplaintId = options.ComplaintId,
                PolicyVersionId = bundle.Policy.VersionId,
                RuleSectionJson = decision.RuleSection == null
                    ? null
                    : JsonSerializer.Serialize(decision.RuleSection, decision.RuleSection.GetType(), CancellationPolicyDefaults.JsonOptions),
                InputsJson = JsonSerializer.Serialize(Inputs(ctx), CancellationPolicyDefaults.JsonOptions),
                ExplanationJson = JsonSerializer.Serialize(decision.Explanation, CancellationPolicyDefaults.JsonOptions),
                GrossValue = decision.GrossValue,
                RefundAmount = decision.RefundAmount,
                FeeAmount = decision.FeeAmount,
                TeacherEarningImpact = decision.TeacherEarningImpact,
                PlatformRevenueImpact = decision.PlatformRevenueImpact,
                Currency = decision.Currency,
                Destination = decision.RefundAmount > 0 ? decision.Destination : null,
                Reason = Truncate(options.Reason, 1000),
                ActorUserId = actor.UserId,
                ActorRole = Truncate(actor.Role, 30) ?? "System",
                CreatedAt = DateTime.UtcNow
            };
            _db.PolicyCases.Add(policyCase);
            await _db.SaveChangesAsync(cancellationToken);

            var feeLeft = decision.FeeAmount;
            foreach (var allocation in decision.Allocations.Where(a => a.Amount > 0))
            {
                var refund = await _refunds.IssueRefundAsync(
                    allocation.PaymentId,
                    enrollment.Id,
                    allocation.Amount,
                    decision.Currency,
                    options.Reason ?? $"Policy: {decision.Kind}",
                    actor.UserId,
                    cancellationToken,
                    decision.Destination,
                    options.ComplaintId,
                    ctx.TargetScheduleId,
                    new RefundPolicyOptions(policyCase.Id, feeLeft));
                feeLeft = 0m;
                policyCase.RefundId ??= refund.Id;
                policyCase.PaymentId ??= allocation.PaymentId;
            }

            foreach (var schedule in enrollment.CourseSchedules.Where(s => decision.CancelScheduleIds.Contains(s.Id)))
            {
                if (schedule.Status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress or ScheduleStatus.Completed))
                    continue;
                schedule.Status = ScheduleStatus.Cancelled;
                schedule.CancellationReason = options.ScheduleReason;
                schedule.PolicyCaseId = policyCase.Id;
            }

            await ApplyTeacherEffectAsync(policyCase, decision, enrollment.ApprovedByTeacherId, actor, cancellationToken);

            if (options.BeforeCommit != null)
                await options.BeforeCommit(policyCase);

            await _db.SaveChangesAsync(cancellationToken);
            if (tx != null)
                await tx.CommitAsync(cancellationToken);
            return policyCase;
        }
        catch
        {
            if (tx != null)
                await tx.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (tx != null)
                await tx.DisposeAsync();
        }
    }

    /// <summary>
    /// Earning lines only exist for delivered sessions. Unpaid lines are voided; lines already in a payout
    /// become a pending deduction so the books are corrected without editing history.
    /// </summary>
    private async Task ApplyTeacherEffectAsync(
        PolicyCase policyCase,
        PolicyDecision decision,
        int teacherId,
        PolicyActor actor,
        CancellationToken cancellationToken)
    {
        var scheduleId = policyCase.CourseScheduleId;
        var effect = decision.TeacherEarningEffect;

        if (scheduleId != null && effect is TeacherEarningEffect.Void or TeacherEarningEffect.Penalty or TeacherEarningEffect.ProRataToRefund)
        {
            var lines = await _db.TeacherEarningLines
                .Where(l => l.CourseScheduleId == scheduleId
                            && l.Status != TeacherEarningLineStatus.Voided)
                .ToListAsync(cancellationToken);

            if (effect == TeacherEarningEffect.ProRataToRefund)
            {
                var deduction = Math.Min(Math.Abs(decision.TeacherEarningImpact), lines.Sum(l => l.Amount));
                if (deduction > 0)
                    AddDeduction(teacherId, deduction, policyCase, "POLICY_EARNING_PRORATA", lines.FirstOrDefault()?.Id, actor);
            }
            else
            {
                foreach (var line in lines)
                {
                    if (line.Status == TeacherEarningLineStatus.IncludedInPayout)
                        AddDeduction(teacherId, line.Amount, policyCase, "POLICY_EARNING_CLAWBACK", line.Id, actor);
                    else
                        line.Status = TeacherEarningLineStatus.Voided;
                }
            }
        }

        if (effect == TeacherEarningEffect.Penalty && decision.TeacherPenaltyAmount > 0)
            AddDeduction(teacherId, decision.TeacherPenaltyAmount, policyCase, "POLICY_TEACHER_PENALTY", null, actor);
    }

    private void AddDeduction(int teacherId, decimal amount, PolicyCase policyCase, string code, int? earningLineId, PolicyActor actor)
    {
        _db.TeacherBalanceAdjustments.Add(new TeacherBalanceAdjustment
        {
            TeacherId = teacherId,
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            Currency = policyCase.Currency,
            Kind = TeacherBalanceAdjustmentKind.Deduction,
            Status = TeacherBalanceAdjustmentStatus.Pending,
            ReasonCode = code,
            ReasonText = $"Policy case #{policyCase.Id} ({policyCase.Kind})",
            RelatedRefundId = policyCase.RefundId,
            RelatedEarningLineId = earningLineId,
            RelatedComplaintId = policyCase.ComplaintId,
            PolicyCaseId = policyCase.Id,
            CreatedByUserId = actor.UserId
        });
    }

    public PolicyPreviewDto ToPreview(PolicyContextBundle bundle, PolicyDecision decision)
    {
        var dto = new PolicyPreviewDto();
        Fill(dto, bundle, decision);
        return dto;
    }

    public PolicyOutcomeDto ToOutcome(PolicyContextBundle bundle, PolicyDecision decision, PolicyCase policyCase)
    {
        var dto = new PolicyOutcomeDto
        {
            CaseId = policyCase.Id,
            RefundId = policyCase.RefundId,
            ReplacementScheduleId = policyCase.ReplacementScheduleId,
            CreatedAt = policyCase.CreatedAt
        };
        Fill(dto, bundle, decision);
        return dto;
    }

    private static void Fill(PolicyPreviewDto dto, PolicyContextBundle bundle, PolicyDecision d)
    {
        dto.Allowed = d.Allowed;
        dto.DenyCode = d.DenyCode;
        dto.Kind = d.Kind.ToString();
        dto.PolicyVersionNumber = bundle.Policy.VersionNumber;
        dto.AmountPaid = bundle.Context.TotalPaid;
        dto.GrossValue = d.GrossValue;
        dto.RefundAmount = d.RefundAmount;
        dto.FeeAmount = d.FeeAmount;
        dto.Currency = d.Currency;
        dto.Destination = d.Destination.ToString();
        dto.CancelsEnrollment = d.CancelEnrollment;
        dto.CancelledSessionsCount = d.CancelScheduleIds.Count;
        dto.CreatesReplacement = d.CreateReplacement;
        dto.Reschedule = d.Reschedule;
        dto.SessionConsideredUsed = d.SessionConsideredUsed;
        dto.Explanation = d.Explanation.Select(l => new PolicyExplanationDto { Ar = l.Ar, En = l.En }).ToList();
    }

    private static object Inputs(PolicyContext ctx)
    {
        var target = ctx.Target;
        return new
        {
            ctx.NowUtc,
            ctx.TotalPaid,
            ctx.TotalRefundable,
            ctx.IsFreeTrial,
            ctx.IsGroup,
            ctx.HasStarted,
            ctx.FirstSessionStartUtc,
            ctx.PackageSessionCount,
            ctx.PackageMinutes,
            ctx.TeacherShareRatio,
            SessionsCompleted = ctx.Sessions.Count(s => s.Usage == PolicySessionUsage.Completed),
            SessionsStudentNoShow = ctx.Sessions.Count(s => s.Usage == PolicySessionUsage.StudentNoShow),
            SessionsUpcoming = ctx.Sessions.Count(s => s.Usage == PolicySessionUsage.Upcoming),
            ctx.TargetScheduleId,
            TargetStartUtc = target?.StartUtc,
            HoursToStart = target?.StartUtc is DateTime start ? Math.Round((start - ctx.NowUtc).TotalHours, 2) : (double?)null,
            Choice = ctx.Choice.ToString(),
            Payments = ctx.Payments.Select(p => new { p.PaymentId, p.Paid, p.AlreadyRefunded })
        };
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
