using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;

namespace Qalam.Service.Mappers;

/// <summary>
/// Upfront earnings projection for interview-pending free-trial enrollments.
/// Uses frozen snapshot hourly/minutes + starter level share — never depends on session completion.
/// </summary>
public static class EnrollmentEarningsProjectionHelper
{
    public sealed record Projection(
        bool IsInterviewPendingAtQuote,
        decimal ProjectedTeacherSharePct,
        decimal ProjectedTeacherEarningsDue,
        decimal ProjectedFreeSessionTeacherDeduction,
        decimal ProjectedPerSessionTeacherValue,
        decimal ProjectedPlatformShare);

    public static bool IsInterviewPendingAtQuote(Enrollment enrollment)
    {
        var snap = enrollment.PricingSnapshot;
        return enrollment.IsFreeTrial && snap != null && snap.TeacherSharePct <= 0m;
    }

    public static Projection? Compute(
        Enrollment enrollment,
        decimal starterSharePct,
        int freeSessionsCount = 0,
        int paidSessionsCount = 0)
    {
        if (!IsInterviewPendingAtQuote(enrollment) || starterSharePct <= 0m)
            return null;

        var snap = enrollment.PricingSnapshot!;
        var schedules = (enrollment.CourseSchedules ?? [])
            .Where(s => s.Status != ScheduleStatus.Cancelled && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToList();

        if (freeSessionsCount <= 0 && enrollment.IsFreeTrial && schedules.Count > 0)
            freeSessionsCount = 1;
        if (paidSessionsCount <= 0)
            paidSessionsCount = Math.Max(0, schedules.Count - freeSessionsCount);

        var totalMinutes = snap.TotalMinutes > 0
            ? snap.TotalMinutes
            : schedules.Sum(s => s.DurationMinutes);
        if (totalMinutes <= 0)
            return null;

        var firstMinutes = schedules.FirstOrDefault()?.DurationMinutes ?? 0;
        if (firstMinutes <= 0)
            firstMinutes = totalMinutes / Math.Max(1, schedules.Count);
        if (firstMinutes <= 0)
            firstMinutes = 60;

        var hourly = snap.EarningsPricePerHour ?? snap.PricePerHour;
        if (hourly <= 0)
            return null;

        var notionalTeacher = Math.Round(
            hourly * totalMinutes / 60m * (starterSharePct / 100m),
            2,
            MidpointRounding.AwayFromZero);

        var firstDeduction = Math.Round(
            notionalTeacher * firstMinutes / (decimal)totalMinutes,
            2,
            MidpointRounding.AwayFromZero);

        var projectedDue = Math.Max(0m, Math.Round(
            notionalTeacher - firstDeduction,
            2,
            MidpointRounding.AwayFromZero));

        var perSession = paidSessionsCount > 0
            ? Math.Round(projectedDue / paidSessionsCount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var amountDue = enrollment.AmountDue > 0 ? enrollment.AmountDue : snap.TotalPrice;
        var projectedPlatform = Math.Round(
            amountDue - projectedDue,
            2,
            MidpointRounding.AwayFromZero);

        return new Projection(
            IsInterviewPendingAtQuote: true,
            ProjectedTeacherSharePct: starterSharePct,
            ProjectedTeacherEarningsDue: projectedDue,
            ProjectedFreeSessionTeacherDeduction: firstDeduction,
            ProjectedPerSessionTeacherValue: perSession,
            ProjectedPlatformShare: projectedPlatform);
    }

    /// <summary>
    /// Package earnings for accrual when snapshot was frozen at 0% interview-pending.
    /// </summary>
    public static decimal ResolvePackageEarningsForAccrual(
        Enrollment enrollment,
        PricingSnapshot snapshot,
        decimal starterSharePct)
    {
        if (snapshot.TeacherEarnings > 0m)
            return snapshot.TeacherEarnings;

        if (!IsInterviewPendingAtQuote(enrollment) || starterSharePct <= 0m)
            return 0m;

        return Compute(enrollment, starterSharePct)?.ProjectedTeacherEarningsDue ?? 0m;
    }
}
