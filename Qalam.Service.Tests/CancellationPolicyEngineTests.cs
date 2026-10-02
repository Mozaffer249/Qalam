using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Implementations;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Tests;

public class CancellationPolicyEngineTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private readonly CancellationPolicyEngine _engine = new();

    private static PolicyContext Package(int sessions, decimal paid, int completed = 0, int studentNoShow = 0)
    {
        var ctx = new PolicyContext
        {
            NowUtc = Now,
            PackageSessionCount = sessions,
            PackageMinutes = sessions * 60,
            TeacherShareRatio = 0.25m,
            Payments = { new PolicyPaymentInfo { PaymentId = 1, Paid = paid } },
            HasStarted = completed + studentNoShow > 0,
            FirstSessionStartUtc = Now.AddDays(1)
        };
        for (var i = 0; i < sessions; i++)
        {
            var usage = i < completed ? PolicySessionUsage.Completed
                : i < completed + studentNoShow ? PolicySessionUsage.StudentNoShow
                : PolicySessionUsage.Upcoming;
            ctx.Sessions.Add(new PolicySessionInfo
            {
                ScheduleId = 100 + i,
                DurationMinutes = 60,
                Usage = usage,
                StartUtc = Now.AddDays(i + 1)
            });
        }
        return ctx;
    }

    [Fact]
    public void BeforeFirstSession_DefaultsRefundFullAmountToWallet_AndCancelsAllSessions()
    {
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.Allowed);
        Assert.Equal(PolicyCaseKind.BeforeFirstSessionCancel, d.Kind);
        Assert.Equal(800m, d.RefundAmount);
        Assert.Equal(RefundDestination.Wallet, d.Destination);
        Assert.Equal(8, d.CancelScheduleIds.Count);
        Assert.True(d.CancelEnrollment);
        Assert.Equal(-200m, d.TeacherEarningImpact);
        Assert.Equal(-600m, d.PlatformRevenueImpact);
    }

    [Fact]
    public void BeforeFirstSession_AppliesPercentAndFee()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.BeforeFirstSession.RefundPct = 90m;
        rules.BeforeFirstSession.FixedFee = 20m;
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(700m, d.RefundAmount);
        Assert.Equal(20m, d.FeeAmount);
    }

    [Fact]
    public void BeforeFirstSession_Disabled_Denies()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.BeforeFirstSession.Enabled = false;
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, rules);

        Assert.False(d.Allowed);
        Assert.Equal(PolicyDenyCodes.CancelDisabled, d.DenyCode);
    }

    [Fact]
    public void BeforeFirstSession_InsideMinHours_Denies()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.BeforeFirstSession.MinHoursBeforeFirstSession = 48;
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(PolicyDenyCodes.CancelWindowClosed, d.DenyCode);
    }

    [Fact]
    public void AfterFirstSession_DisabledByDefault()
    {
        var ctx = Package(8, 800m, completed: 1);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.False(d.Allowed);
        Assert.Equal(PolicyDenyCodes.CancelAfterStartDisabled, d.DenyCode);
    }

    [Fact]
    public void AfterFirstSession_EightSessions800_OneCompleted_Refunds700MinusFee()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.AfterFirstSession.Enabled = true;
        rules.AfterFirstSession.FixedFee = 50m;
        var ctx = Package(8, 800m, completed: 1);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, rules);

        Assert.True(d.Allowed);
        Assert.Equal(PolicyCaseKind.AfterFirstSessionCancel, d.Kind);
        Assert.Equal(700m, d.GrossValue);
        Assert.Equal(650m, d.RefundAmount);
        Assert.Equal(50m, d.FeeAmount);
        Assert.Equal(7, d.CancelScheduleIds.Count);
    }

    [Fact]
    public void AfterFirstSession_StudentNoShowNotCounted_WhenConfigured()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.AfterFirstSession.Enabled = true;
        rules.AfterFirstSession.CountStudentNoShowAsUsed = false;
        var ctx = Package(8, 800m, completed: 1, studentNoShow: 1);
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(700m, d.RefundAmount);
    }

    [Fact]
    public void AfterFirstSession_CappedAtRemainingRefundable()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.AfterFirstSession.Enabled = true;
        var ctx = Package(8, 800m, completed: 1);
        ctx.Payments[0].AlreadyRefunded = 300m;
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(500m, d.RefundAmount);
    }

    [Fact]
    public void GroupRefund_IsSplitAcrossPaymentsProportionally()
    {
        var ctx = Package(4, 0m);
        ctx.Payments.Clear();
        ctx.Payments.Add(new PolicyPaymentInfo { PaymentId = 1, Paid = 300m });
        ctx.Payments.Add(new PolicyPaymentInfo { PaymentId = 2, Paid = 100m });
        ctx.IsGroup = true;
        ctx.Kind = PolicyCaseKind.BeforeFirstSessionCancel;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.Equal(400m, d.RefundAmount);
        Assert.Equal(300m, d.Allocations.Single(a => a.PaymentId == 1).Amount);
        Assert.Equal(100m, d.Allocations.Single(a => a.PaymentId == 2).Amount);
    }

    [Fact]
    public void SessionCancel_InWindow_StudentChoiceRefund_RefundsSessionValue()
    {
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 101;
        ctx.Choice = PolicyStudentChoice.Refund;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.Allowed);
        Assert.Equal(100m, d.RefundAmount);
        Assert.Equal(new[] { 101 }, d.CancelScheduleIds);
        Assert.False(d.Reschedule);
    }

    [Fact]
    public void SessionCancel_InWindow_Reschedule_NoRefund()
    {
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 101;
        ctx.Choice = PolicyStudentChoice.Reschedule;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.Reschedule);
        Assert.Equal(0m, d.RefundAmount);
        Assert.Empty(d.CancelScheduleIds);
    }

    [Fact]
    public void SessionCancel_ExactlyAtNoticeBoundary_IsInWindow()
    {
        var ctx = Package(8, 800m);
        ctx.Sessions[0].StartUtc = Now.AddHours(12);
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 100;
        ctx.Choice = PolicyStudentChoice.Refund;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.Equal(100m, d.RefundAmount);
    }

    [Fact]
    public void SessionCancel_Late_DefaultConsideredUsed()
    {
        var ctx = Package(8, 800m);
        ctx.Sessions[0].StartUtc = Now.AddHours(11);
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.Allowed);
        Assert.True(d.SessionConsideredUsed);
        Assert.Equal(0m, d.RefundAmount);
    }

    [Fact]
    public void SessionCancel_Late_PartialRefund()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.SessionCancellation.LateOutcome = LateSessionOutcome.PartialRefund;
        rules.SessionCancellation.LateRefundPct = 50m;
        var ctx = Package(8, 800m);
        ctx.Sessions[0].StartUtc = Now.AddHours(2);
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(50m, d.RefundAmount);
    }

    [Fact]
    public void SessionCancel_LateReschedule_Denied()
    {
        var ctx = Package(8, 800m);
        ctx.Sessions[0].StartUtc = Now.AddHours(2);
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 100;
        ctx.Choice = PolicyStudentChoice.Reschedule;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.Equal(PolicyDenyCodes.RescheduleWindowClosed, d.DenyCode);
    }

    [Fact]
    public void SessionCancel_Group_Denied()
    {
        var ctx = Package(8, 800m);
        ctx.IsGroup = true;
        ctx.Kind = PolicyCaseKind.SessionCancel;
        ctx.TargetScheduleId = 101;

        Assert.Equal(PolicyDenyCodes.GroupSessionCancel, _engine.Evaluate(ctx, CancellationPolicyDefaults.Create()).DenyCode);
    }

    [Fact]
    public void TeacherNoShow_DefaultRefundsFullSessionValue_AndTeacherLosesEarning()
    {
        var ctx = Package(8, 800m, completed: 1);
        ctx.Sessions[1].Usage = PolicySessionUsage.TeacherNoShow;
        ctx.Kind = PolicyCaseKind.TeacherNoShow;
        ctx.TargetScheduleId = 101;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.Equal(100m, d.RefundAmount);
        Assert.False(d.CreateReplacement);
        Assert.Equal(-25m, d.TeacherEarningImpact);
        Assert.Equal(-75m, d.PlatformRevenueImpact);
        Assert.Equal(TeacherEarningEffect.Void, d.TeacherEarningEffect);
    }

    [Fact]
    public void TeacherNoShow_RefundAndReplacement_WithPenalty()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.TeacherNoShow.Outcome = TeacherNoShowOutcome.RefundAndReplacement;
        rules.TeacherNoShow.TeacherPenaltyAmount = 10m;
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.TeacherNoShow;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(100m, d.RefundAmount);
        Assert.True(d.CreateReplacement);
        Assert.Equal(-10m, d.TeacherEarningImpact);
    }

    [Fact]
    public void TeacherNoShow_FreeTrial_GetsReplacementNotRefund()
    {
        var ctx = Package(1, 0m);
        ctx.IsFreeTrial = true;
        ctx.Kind = PolicyCaseKind.TeacherNoShow;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.CreateReplacement);
        Assert.Equal(0m, d.RefundAmount);
    }

    [Fact]
    public void StudentNoShow_DefaultConsideredUsed_NoRefund()
    {
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.StudentNoShow;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.Allowed);
        Assert.True(d.SessionConsideredUsed);
        Assert.Equal(0m, d.RefundAmount);
        Assert.Equal(TeacherEarningEffect.Keep, d.TeacherEarningEffect);
    }

    [Fact]
    public void StudentNoShow_PartialRefund_ProRataTeacherEffect()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.StudentNoShow.Outcome = StudentNoShowOutcome.PartialRefund;
        rules.StudentNoShow.RefundPct = 40m;
        rules.StudentNoShow.TeacherEarningEffect = TeacherEarningEffect.ProRataToRefund;
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.StudentNoShow;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, rules);

        Assert.Equal(40m, d.RefundAmount);
        Assert.Equal(-10m, d.TeacherEarningImpact);
    }

    [Fact]
    public void TechnicalIssue_DefaultReplacement()
    {
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.TechnicalIssue;
        ctx.TargetScheduleId = 100;

        var d = _engine.Evaluate(ctx, CancellationPolicyDefaults.Create());

        Assert.True(d.CreateReplacement);
        Assert.Equal(0m, d.RefundAmount);
    }

    [Fact]
    public void TechnicalIssue_PartialRefund()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.TechnicalIssue.Outcome = TechnicalIssueOutcome.PartialRefund;
        rules.TechnicalIssue.RefundPct = 50m;
        var ctx = Package(8, 800m);
        ctx.Kind = PolicyCaseKind.TechnicalIssue;
        ctx.TargetScheduleId = 100;

        Assert.Equal(50m, _engine.Evaluate(ctx, rules).RefundAmount);
    }

    [Fact]
    public void AdminException_Validation()
    {
        var rules = CancellationPolicyDefaults.Create().AdminExceptions;

        Assert.Equal(PolicyDenyCodes.ExceptionReasonTooShort,
            _engine.ValidateException(rules, AdminExceptionAction.FullRefund, 10m, "short", false).DenyCode);
        Assert.Equal(PolicyDenyCodes.ExceptionAmountNeedsSuperAdmin,
            _engine.ValidateException(rules, AdminExceptionAction.WalletCredit, 5000m, "Goodwill credit for outage", false).DenyCode);
        Assert.True(_engine.ValidateException(rules, AdminExceptionAction.WalletCredit, 5000m, "Goodwill credit for outage", true).Allowed);

        rules.AllowedActions.Remove(AdminExceptionAction.Reschedule);
        Assert.Equal(PolicyDenyCodes.ExceptionActionNotAllowed,
            _engine.ValidateException(rules, AdminExceptionAction.Reschedule, 0m, "Student requested a new time", true).DenyCode);
    }

    [Fact]
    public void Rules_RoundTripJson_KeepsValues()
    {
        var rules = CancellationPolicyDefaults.Create();
        rules.SessionCancellation.NoticeHours = 24;
        rules.TeacherNoShow.Outcome = TeacherNoShowOutcome.StudentChoice;

        var back = CancellationPolicyDefaults.FromJson(CancellationPolicyDefaults.ToJson(rules));

        Assert.Equal(24, back.SessionCancellation.NoticeHours);
        Assert.Equal(TeacherNoShowOutcome.StudentChoice, back.TeacherNoShow.Outcome);
        Assert.Equal(CancellationPolicyDefaults.Create().AdminExceptions.AllowedActions.Count, back.AdminExceptions.AllowedActions.Count);
    }
}
