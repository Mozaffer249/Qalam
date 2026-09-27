using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Mappers;

/// <summary>
/// Display projection for legacy snapshots frozen at 0% teacher share (quoted while the old
/// per-domain interview was pending). Projects the full package at the starter level share —
/// the student's free trial never reduces teacher earnings. Admin recompute replaces these snapshots.
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
        return snap != null && snap.TeacherSharePct <= 0m;
    }

    public static Projection? Compute(Enrollment enrollment, decimal starterSharePct)
    {
        if (!IsInterviewPendingAtQuote(enrollment) || starterSharePct <= 0m)
            return null;

        var snap = enrollment.PricingSnapshot!;
        var schedules = (enrollment.CourseSchedules ?? [])
            .Where(s => s.Status != ScheduleStatus.Cancelled && s.Status != ScheduleStatus.Rescheduled)
            .ToList();

        var totalMinutes = snap.TotalMinutes > 0
            ? snap.TotalMinutes
            : schedules.Sum(s => s.DurationMinutes);
        if (totalMinutes <= 0)
            return null;

        var hourly = snap.EarningsPricePerHour ?? snap.PricePerHour;
        if (hourly <= 0)
            return null;

        var projectedDue = Math.Round(
            hourly * totalMinutes / 60m * (starterSharePct / 100m),
            2,
            MidpointRounding.AwayFromZero);

        var perSession = schedules.Count > 0
            ? Math.Round(projectedDue / schedules.Count, 2, MidpointRounding.AwayFromZero)
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
            ProjectedFreeSessionTeacherDeduction: 0m,
            ProjectedPerSessionTeacherValue: perSession,
            ProjectedPlatformShare: projectedPlatform);
    }
}
