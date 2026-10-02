using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Implementations;

/// <summary>
/// Pure cancellation &amp; refund calculator. Same code path for previews and applied cases.
/// </summary>
public class CancellationPolicyEngine : ICancellationPolicyEngine
{
    public PolicyDecision Evaluate(PolicyContext ctx, CancellationPolicyRules rules)
    {
        var decision = ctx.Kind switch
        {
            PolicyCaseKind.BeforeFirstSessionCancel or PolicyCaseKind.AfterFirstSessionCancel =>
                ctx.HasStarted
                    ? AfterFirstSession(ctx, rules.AfterFirstSession)
                    : BeforeFirstSession(ctx, rules.BeforeFirstSession),
            PolicyCaseKind.SessionCancel => SessionCancel(ctx, rules.SessionCancellation),
            PolicyCaseKind.TeacherNoShow or PolicyCaseKind.TeacherSessionCancel => TeacherNoShow(ctx, rules.TeacherNoShow),
            PolicyCaseKind.StudentNoShow => StudentNoShow(ctx, rules.StudentNoShow),
            PolicyCaseKind.TechnicalIssue => TechnicalIssue(ctx, rules.TechnicalIssue),
            _ => PolicyDecision.Deny(ctx.Kind, PolicyDenyCodes.RuleDisabled, "نوع الحالة غير مدعوم.", "Unsupported case kind.")
        };

        decision.Currency = ctx.Currency;
        if (decision.Allowed)
            Finalize(ctx, decision);
        return decision;
    }

    public PolicyDecision ValidateException(
        AdminExceptionRules rules,
        AdminExceptionAction action,
        decimal amount,
        string? reason,
        bool isSuperAdmin)
    {
        const PolicyCaseKind kind = PolicyCaseKind.AdminException;
        if (!rules.Enabled)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.ExceptionsDisabled,
                "الاستثناءات الإدارية معطلة في السياسة.", "Admin exceptions are disabled by the policy.");
        if (!rules.AllowedActions.Contains(action))
            return PolicyDecision.Deny(kind, PolicyDenyCodes.ExceptionActionNotAllowed,
                "هذا الإجراء غير مسموح في السياسة.", "This action is not allowed by the policy.");
        if ((reason ?? "").Trim().Length < Math.Max(1, rules.MinReasonLength))
            return PolicyDecision.Deny(kind, PolicyDenyCodes.ExceptionReasonTooShort,
                $"السبب مطلوب ({rules.MinReasonLength} أحرف على الأقل).",
                $"A reason of at least {rules.MinReasonLength} characters is required.");
        if (!isSuperAdmin && rules.MaxAmountForAdmin > 0 && Math.Abs(amount) > rules.MaxAmountForAdmin)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.ExceptionAmountNeedsSuperAdmin,
                "المبلغ يتجاوز صلاحية المشرف ويتطلب مشرفاً عاماً.",
                "The amount exceeds the admin limit and needs a SuperAdmin.");
        return new PolicyDecision { Kind = kind, Allowed = true, RuleSection = rules };
    }

    private static PolicyDecision BeforeFirstSession(PolicyContext ctx, BeforeFirstSessionRules r)
    {
        const PolicyCaseKind kind = PolicyCaseKind.BeforeFirstSessionCancel;
        if (!r.Enabled)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.CancelDisabled,
                "إلغاء التسجيل غير متاح حالياً.", "Cancelling this enrollment is not available.");

        if (r.MinHoursBeforeFirstSession > 0 && ctx.FirstSessionStartUtc is DateTime first
            && (first - ctx.NowUtc).TotalHours < r.MinHoursBeforeFirstSession)
        {
            return PolicyDecision.Deny(kind, PolicyDenyCodes.CancelWindowClosed,
                $"يجب الإلغاء قبل {r.MinHoursBeforeFirstSession} ساعة على الأقل من الجلسة الأولى.",
                $"Cancellation must be at least {r.MinHoursBeforeFirstSession} hours before the first session.");
        }

        var gross = ctx.TotalRefundable;
        var d = new PolicyDecision
        {
            Kind = kind,
            Allowed = true,
            GrossValue = gross,
            Destination = r.Destination,
            CancelEnrollment = true,
            RuleSection = r
        };
        ApplyRefund(d, gross, r.RefundPct, r.FixedFee);
        if (r.AutoCancelFutureSessions)
            d.CancelScheduleIds.AddRange(UpcomingIds(ctx));
        d.TeacherEarningImpact = -Round(ctx.TeacherShareRatio * gross);
        d.Explanation.Add(Line(
            $"إلغاء قبل الجلسة الأولى: استرداد {r.RefundPct:0.##}% من المبلغ المدفوع.",
            $"Cancellation before the first session: {r.RefundPct:0.##}% of the amount paid is refunded."));
        return d;
    }

    private static PolicyDecision AfterFirstSession(PolicyContext ctx, AfterFirstSessionRules r)
    {
        const PolicyCaseKind kind = PolicyCaseKind.AfterFirstSessionCancel;
        if (!r.Enabled)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.CancelAfterStartDisabled,
                "لا يمكن إلغاء التسجيل بعد بدء الجلسات.", "The enrollment cannot be cancelled after sessions have started.");

        var upcoming = ctx.Sessions.Where(s => s.Usage == PolicySessionUsage.Upcoming).ToList();
        var counted = new List<PolicySessionInfo>(upcoming);
        if (!r.ExcludeCompletedSessions)
            counted.AddRange(ctx.Sessions.Where(s => s.Usage == PolicySessionUsage.Completed));
        if (!r.CountStudentNoShowAsUsed)
            counted.AddRange(ctx.Sessions.Where(s => s.Usage == PolicySessionUsage.StudentNoShow));

        decimal gross = r.CalculationMethod switch
        {
            RefundCalculationMethod.PercentOfPaid => ctx.TotalRefundable,
            RefundCalculationMethod.UnusedMinutes when ctx.PackageMinutes > 0 =>
                ctx.TotalPaid * counted.Sum(s => s.DurationMinutes) / ctx.PackageMinutes,
            _ => ctx.PackageSessionCount > 0 ? ctx.TotalPaid * counted.Count / ctx.PackageSessionCount : 0m
        };
        gross = Math.Min(Round(gross), ctx.TotalRefundable);

        var d = new PolicyDecision
        {
            Kind = kind,
            Allowed = true,
            GrossValue = gross,
            Destination = r.Destination,
            CancelEnrollment = true,
            RuleSection = r
        };
        d.CancelScheduleIds.AddRange(upcoming.Select(s => s.ScheduleId));
        ApplyRefund(d, gross, r.RefundPct, r.FixedFee);

        var upcomingValue = ctx.PackageSessionCount > 0 ? ctx.TotalPaid * upcoming.Count / ctx.PackageSessionCount : 0m;
        d.TeacherEarningImpact = -Round(ctx.TeacherShareRatio * upcomingValue);

        var used = ctx.Sessions.Count(s => s.Usage is PolicySessionUsage.Completed or PolicySessionUsage.StudentNoShow);
        d.Explanation.Add(Line(
            $"إلغاء بعد بدء الجلسات: يُحسب المتبقي من الجلسات غير المستخدمة ({upcoming.Count} من {ctx.PackageSessionCount}).",
            $"Cancellation after sessions started: the refund covers unused sessions ({upcoming.Count} of {ctx.PackageSessionCount})."));
        if (used > 0 && r.ExcludeCompletedSessions)
            d.Explanation.Add(Line(
                $"الجلسات المستخدمة ({used}) غير مستردة.",
                $"Used sessions ({used}) are not refunded."));
        return d;
    }

    private static PolicyDecision SessionCancel(PolicyContext ctx, SessionCancellationRules r)
    {
        const PolicyCaseKind kind = PolicyCaseKind.SessionCancel;
        if (!r.Enabled)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.SessionCancelDisabled,
                "إلغاء الجلسات غير متاح حالياً.", "Session cancellation is not available.");
        if (ctx.IsGroup)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.GroupSessionCancel,
                "لا يمكن إلغاء جلسة في تسجيل جماعي.", "Sessions of group enrollments cannot be cancelled individually.");
        var target = ctx.Target;
        if (target == null || target.Usage != PolicySessionUsage.Upcoming || target.StartUtc == null)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.SessionNotUpcoming,
                "يمكن إلغاء الجلسات القادمة فقط.", "Only upcoming sessions can be cancelled.");

        var hoursLeft = (target.StartUtc.Value - ctx.NowUtc).TotalHours;
        var inWindow = hoursLeft >= r.NoticeHours;
        var value = SessionValue(ctx, target);

        var d = new PolicyDecision
        {
            Kind = kind,
            Allowed = true,
            GrossValue = value,
            Destination = r.Destination,
            RuleSection = r
        };

        if (inWindow)
        {
            var reschedule = r.InWindowOutcome == InWindowSessionOutcome.Reschedule
                             || (r.InWindowOutcome == InWindowSessionOutcome.StudentChoice && ctx.Choice == PolicyStudentChoice.Reschedule);
            if (reschedule)
            {
                d.Reschedule = true;
                d.GrossValue = 0;
                d.Explanation.Add(Line(
                    $"الإلغاء قبل {r.NoticeHours} ساعة على الأقل: يمكنك إعادة جدولة الجلسة دون خصم.",
                    $"Cancelled at least {r.NoticeHours} hours ahead: you can reschedule the session at no cost."));
                return d;
            }

            if (ctx.Choice == PolicyStudentChoice.Reschedule && r.InWindowOutcome == InWindowSessionOutcome.Refund)
                return PolicyDecision.Deny(kind, PolicyDenyCodes.RescheduleWindowClosed,
                    "السياسة لا تسمح بإعادة الجدولة، الاسترداد فقط.", "The policy allows a refund only, not rescheduling.");

            d.CancelScheduleIds.Add(target.ScheduleId);
            ApplyRefund(d, value, 100m, r.FixedFee);
            d.TeacherEarningImpact = -Round(ctx.TeacherShareRatio * value);
            d.Explanation.Add(Line(
                $"الإلغاء قبل {r.NoticeHours} ساعة على الأقل: استرداد قيمة الجلسة.",
                $"Cancelled at least {r.NoticeHours} hours ahead: the session value is refunded."));
            return d;
        }

        if (ctx.Choice == PolicyStudentChoice.Reschedule)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.RescheduleWindowClosed,
                $"إعادة الجدولة متاحة قبل {r.NoticeHours} ساعة على الأقل من موعد الجلسة.",
                $"Rescheduling is only available at least {r.NoticeHours} hours before the session.");

        d.CancelScheduleIds.Add(target.ScheduleId);
        if (r.LateOutcome == LateSessionOutcome.PartialRefund && r.LateRefundPct > 0)
        {
            ApplyRefund(d, value, r.LateRefundPct, r.FixedFee);
            d.TeacherEarningImpact = -Round(ctx.TeacherShareRatio * d.RefundAmount);
            d.TeacherEarningEffect = TeacherEarningEffect.ProRataToRefund;
            d.Explanation.Add(Line(
                $"إلغاء متأخر (أقل من {r.NoticeHours} ساعة): استرداد {r.LateRefundPct:0.##}% من قيمة الجلسة.",
                $"Late cancellation (under {r.NoticeHours} hours): {r.LateRefundPct:0.##}% of the session value is refunded."));
        }
        else
        {
            d.SessionConsideredUsed = true;
            d.GrossValue = value;
            d.Explanation.Add(Line(
                $"إلغاء متأخر (أقل من {r.NoticeHours} ساعة): تُحتسب الجلسة مستخدمة ولا يوجد استرداد.",
                $"Late cancellation (under {r.NoticeHours} hours): the session counts as used and is not refunded."));
        }
        return d;
    }

    private static PolicyDecision TeacherNoShow(PolicyContext ctx, TeacherNoShowRules r)
    {
        var kind = ctx.Kind;
        if (!r.Enabled)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.RuleDisabled,
                "قاعدة غياب المعلم معطلة؛ ستعالجها الإدارة يدوياً.", "The teacher no-show rule is disabled; an admin will handle it.");
        var target = ctx.Target;
        if (target == null)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.NotFound, "الجلسة غير موجودة.", "Session not found.");

        var value = SessionValue(ctx, target);
        var outcome = r.Outcome;
        if (outcome == TeacherNoShowOutcome.StudentChoice)
            outcome = ctx.Choice == PolicyStudentChoice.Reschedule ? TeacherNoShowOutcome.Replacement : TeacherNoShowOutcome.Refund;
        if (ctx.IsFreeTrial && outcome == TeacherNoShowOutcome.Refund)
            outcome = TeacherNoShowOutcome.Replacement;

        var d = new PolicyDecision
        {
            Kind = kind,
            Allowed = true,
            GrossValue = value,
            Destination = r.Destination,
            RuleSection = r,
            TeacherEarningEffect = r.TeacherEarningEffect,
            TeacherPenaltyAmount = Math.Max(0, r.TeacherPenaltyAmount)
        };
        d.CancelScheduleIds.Add(target.ScheduleId);

        var refund = outcome is TeacherNoShowOutcome.Refund or TeacherNoShowOutcome.RefundAndReplacement;
        d.CreateReplacement = outcome is TeacherNoShowOutcome.Replacement or TeacherNoShowOutcome.RefundAndReplacement;
        if (refund)
            ApplyRefund(d, value, r.RefundPct, 0m);

        // Teacher loses the session earning unless a replacement is delivered; plus any penalty.
        var lost = d.CreateReplacement ? 0m : Round(ctx.TeacherShareRatio * value);
        d.TeacherEarningImpact = -(lost + d.TeacherPenaltyAmount);

        d.Explanation.Add(kind == PolicyCaseKind.TeacherSessionCancel
            ? Line("ألغى المعلم الجلسة؛ لن تخسر قيمتها.", "The teacher cancelled the session; you will not lose its value.")
            : Line("لم يحضر المعلم الجلسة؛ لن تخسر قيمتها.", "The teacher did not attend; you will not lose the session value."));
        if (refund)
            d.Explanation.Add(Line($"استرداد {r.RefundPct:0.##}% من قيمة الجلسة.", $"{r.RefundPct:0.##}% of the session value is refunded."));
        if (d.CreateReplacement)
            d.Explanation.Add(Line("تم ترتيب جلسة بديلة.", "A replacement session is scheduled."));
        return d;
    }

    private static PolicyDecision StudentNoShow(PolicyContext ctx, StudentNoShowRules r)
    {
        const PolicyCaseKind kind = PolicyCaseKind.StudentNoShow;
        var target = ctx.Target;
        if (target == null)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.NotFound, "الجلسة غير موجودة.", "Session not found.");

        var value = SessionValue(ctx, target);
        var d = new PolicyDecision
        {
            Kind = kind,
            Allowed = true,
            GrossValue = value,
            Destination = r.Destination,
            RuleSection = r,
            TeacherEarningEffect = r.Enabled ? r.TeacherEarningEffect : TeacherEarningEffect.Keep,
            SessionConsideredUsed = !r.Enabled || r.ConsideredUsed
        };

        if (!r.Enabled || r.Outcome == StudentNoShowOutcome.NoRefund)
        {
            d.Explanation.Add(Line(
                "لم يحضر الطالب الجلسة ولم يُلغِها في الوقت المحدد؛ تُحتسب الجلسة مستخدمة.",
                "The student did not attend or cancel in time; the session counts as used."));
            return d;
        }

        if (r.Outcome == StudentNoShowOutcome.PartialRefund && r.RefundPct > 0 && !ctx.IsFreeTrial)
        {
            ApplyRefund(d, value, r.RefundPct, 0m);
            d.TeacherEarningImpact = r.TeacherEarningEffect switch
            {
                TeacherEarningEffect.Void => -Round(ctx.TeacherShareRatio * value),
                TeacherEarningEffect.ProRataToRefund => -Round(ctx.TeacherShareRatio * d.RefundAmount),
                _ => 0m
            };
            d.Explanation.Add(Line(
                $"غياب الطالب: استرداد {r.RefundPct:0.##}% من قيمة الجلسة.",
                $"Student no-show: {r.RefundPct:0.##}% of the session value is refunded."));
            return d;
        }

        d.CreateReplacement = true;
        d.SessionConsideredUsed = false;
        d.Explanation.Add(Line("غياب الطالب: تمت إعادة جدولة الجلسة.", "Student no-show: the session is rescheduled."));
        return d;
    }

    private static PolicyDecision TechnicalIssue(PolicyContext ctx, TechnicalIssueRules r)
    {
        const PolicyCaseKind kind = PolicyCaseKind.TechnicalIssue;
        if (!r.Enabled)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.RuleDisabled,
                "قاعدة المشاكل التقنية معطلة.", "The technical issue rule is disabled.");
        var target = ctx.Target;
        if (target == null)
            return PolicyDecision.Deny(kind, PolicyDenyCodes.NotFound, "الجلسة غير موجودة.", "Session not found.");

        var value = SessionValue(ctx, target);
        var d = new PolicyDecision
        {
            Kind = kind,
            Allowed = true,
            GrossValue = value,
            Destination = r.Destination,
            RuleSection = r,
            TeacherEarningEffect = r.TeacherEarningEffect
        };

        switch (r.Outcome)
        {
            case TechnicalIssueOutcome.FullRefund:
                ApplyRefund(d, value, 100m, 0m);
                break;
            case TechnicalIssueOutcome.PartialRefund:
                ApplyRefund(d, value, r.RefundPct, 0m);
                break;
            default:
                d.CreateReplacement = true;
                break;
        }

        d.TeacherEarningImpact = r.TeacherEarningEffect switch
        {
            TeacherEarningEffect.Void when !d.CreateReplacement => -Round(ctx.TeacherShareRatio * value),
            TeacherEarningEffect.ProRataToRefund => -Round(ctx.TeacherShareRatio * d.RefundAmount),
            _ => 0m
        };
        d.Explanation.Add(d.CreateReplacement
            ? Line("مشكلة تقنية: تم ترتيب جلسة بديلة.", "Technical issue: a replacement session is scheduled.")
            : Line($"مشكلة تقنية: استرداد {d.RefundAmount:0.##} {ctx.Currency}.", $"Technical issue: {d.RefundAmount:0.##} {ctx.Currency} refunded."));
        return d;
    }

    private static void ApplyRefund(PolicyDecision d, decimal gross, decimal pct, decimal fee)
    {
        pct = Math.Clamp(pct, 0m, 100m);
        fee = Math.Max(0m, fee);
        var beforeFee = Round(gross * pct / 100m);
        var appliedFee = Math.Min(fee, beforeFee);
        d.FeeAmount = Round(appliedFee);
        d.RefundAmount = Round(beforeFee - appliedFee);
        if (fee > 0)
            d.Explanation.Add(Line($"رسوم إلغاء: {fee:0.##}.", $"Cancellation fee: {fee:0.##}."));
    }

    private static void Finalize(PolicyContext ctx, PolicyDecision d)
    {
        if (ctx.IsFreeTrial)
        {
            d.RefundAmount = 0;
            d.FeeAmount = 0;
        }

        d.RefundAmount = Math.Min(d.RefundAmount, ctx.TotalRefundable);
        d.Allocations = Allocate(ctx.Payments, d.RefundAmount);
        // Money leaving = refund; teacher gives up |TeacherEarningImpact|; the platform covers the rest.
        d.PlatformRevenueImpact = Round(-d.RefundAmount - d.TeacherEarningImpact);
    }

    /// <summary>Splits a refund across payments in proportion to what is still refundable on each.</summary>
    public static List<PolicyRefundAllocation> Allocate(IReadOnlyList<PolicyPaymentInfo> payments, decimal amount)
    {
        var result = new List<PolicyRefundAllocation>();
        var refundable = payments.Where(p => p.Refundable > 0).ToList();
        var total = refundable.Sum(p => p.Refundable);
        if (amount <= 0 || total <= 0)
            return result;

        var remaining = Math.Min(amount, total);
        for (var i = 0; i < refundable.Count; i++)
        {
            var p = refundable[i];
            var share = i == refundable.Count - 1
                ? remaining
                : Math.Min(p.Refundable, Round(amount * p.Refundable / total));
            share = Math.Min(share, p.Refundable);
            if (share <= 0) continue;
            result.Add(new PolicyRefundAllocation { PaymentId = p.PaymentId, Amount = share });
            remaining -= share;
        }
        return result;
    }

    private static decimal SessionValue(PolicyContext ctx, PolicySessionInfo session)
    {
        decimal value;
        if (ctx.PackageMinutes > 0 && session.DurationMinutes > 0)
            value = ctx.TotalPaid * session.DurationMinutes / ctx.PackageMinutes;
        else if (ctx.PackageSessionCount > 0)
            value = ctx.TotalPaid / ctx.PackageSessionCount;
        else
            value = 0m;
        return Math.Min(Round(value), ctx.TotalRefundable);
    }

    private static IEnumerable<int> UpcomingIds(PolicyContext ctx)
        => ctx.Sessions.Where(s => s.Usage == PolicySessionUsage.Upcoming).Select(s => s.ScheduleId);

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static PolicyExplanationLine Line(string ar, string en) => new() { Ar = ar, En = en };
}
