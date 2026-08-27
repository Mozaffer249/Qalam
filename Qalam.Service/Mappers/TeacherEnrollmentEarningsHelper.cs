using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Mappers;

/// <summary>
/// Computes teacher-facing earnings breakdown for an enrollment from snapshot + ledger lines.
/// </summary>
public static class TeacherEnrollmentEarningsHelper
{
    public sealed record EarningLineInfo(
        TeacherEarningLineStatus Status,
        PayoutBatchStatus? BatchStatus,
        decimal Amount);

    public sealed record EarningsBreakdown(
        decimal TeacherEarningsDue,
        decimal PlatformCommission,
        decimal TeacherSharePct,
        int FreeSessionsCount,
        int PaidSessionsCount,
        decimal PerSessionTeacherValue,
        decimal FreeSessionTeacherDeduction,
        decimal AccruedNet,
        string EarningUiStatus,
        bool IsInterviewPendingAtQuote,
        decimal ProjectedTeacherSharePct,
        decimal ProjectedTeacherEarningsDue,
        decimal ProjectedFreeSessionTeacherDeduction,
        decimal ProjectedPerSessionTeacherValue);

    public static EarningsBreakdown Compute(
        Enrollment enrollment,
        IReadOnlyList<EarningLineInfo> lines,
        decimal starterSharePct = 0m)
    {
        var snap = enrollment.PricingSnapshot;
        var schedules = (enrollment.CourseSchedules ?? [])
            .Where(s => s.Status != ScheduleStatus.Cancelled && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToList();

        var freeSessions = enrollment.IsFreeTrial && schedules.Count > 0 ? 1 : 0;
        var paidSessions = Math.Max(0, schedules.Count - freeSessions);

        var teacherDue = snap?.TeacherEarnings ?? 0m;
        var platformCommission = snap?.PlatformShare ?? 0m;
        var sharePct = snap?.TeacherSharePct ?? 0m;

        var deduction = 0m;
        if (enrollment.IsFreeTrial && sharePct > 0)
        {
            var totalMinutes = snap?.TotalMinutes > 0
                ? snap.TotalMinutes
                : schedules.Sum(s => s.DurationMinutes);
            var firstMinutes = schedules.FirstOrDefault()?.DurationMinutes ?? 0;
            if (firstMinutes <= 0)
                firstMinutes = 60;
            var earnable = totalMinutes > firstMinutes ? totalMinutes - firstMinutes : 0;
            if (teacherDue > 0 && earnable > 0)
            {
                deduction = Math.Round(
                    teacherDue * firstMinutes / (decimal)earnable,
                    2,
                    MidpointRounding.AwayFromZero);
            }
            else
            {
                var hourly = snap?.EarningsPricePerHour ?? snap?.PricePerHour ?? 0m;
                deduction = Math.Round(
                    hourly * firstMinutes / 60m * (sharePct / 100m),
                    2,
                    MidpointRounding.AwayFromZero);
            }
        }

        var perSession = paidSessions > 0
            ? Math.Round(teacherDue / paidSessions, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var accrued = lines
            .Where(l => l.Status != TeacherEarningLineStatus.Voided)
            .Sum(l => l.Amount);

        var projection = EnrollmentEarningsProjectionHelper.Compute(
            enrollment,
            starterSharePct,
            freeSessions,
            paidSessions);

        return new EarningsBreakdown(
            TeacherEarningsDue: teacherDue,
            PlatformCommission: platformCommission,
            TeacherSharePct: sharePct,
            FreeSessionsCount: freeSessions,
            PaidSessionsCount: paidSessions,
            PerSessionTeacherValue: perSession,
            FreeSessionTeacherDeduction: deduction,
            AccruedNet: accrued,
            EarningUiStatus: ResolveUiStatus(lines),
            IsInterviewPendingAtQuote: projection?.IsInterviewPendingAtQuote ?? false,
            ProjectedTeacherSharePct: projection?.ProjectedTeacherSharePct ?? 0m,
            ProjectedTeacherEarningsDue: projection?.ProjectedTeacherEarningsDue ?? 0m,
            ProjectedFreeSessionTeacherDeduction: projection?.ProjectedFreeSessionTeacherDeduction ?? 0m,
            ProjectedPerSessionTeacherValue: projection?.ProjectedPerSessionTeacherValue ?? 0m);
    }

    public static string ResolveUiStatus(IReadOnlyList<EarningLineInfo> lines)
    {
        if (lines.Count == 0)
            return "Pending";

        if (lines.Any(l => l.Status == TeacherEarningLineStatus.Pending))
            return "Available";

        var included = lines
            .Where(l => l.Status == TeacherEarningLineStatus.IncludedInPayout)
            .ToList();
        if (included.Count > 0)
        {
            if (included.Any(l => l.BatchStatus == PayoutBatchStatus.Paid))
                return "Paid";
            return "Pending";
        }

        if (lines.All(l => l.Status == TeacherEarningLineStatus.Voided))
            return "Refunded";

        return "Pending";
    }
}
