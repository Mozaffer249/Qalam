using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Service.Implementations;

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

    /// <param name="teacherInterviewScheduleId">
    /// The teacher's account-level unpaid interview schedule (<c>Teacher.InterviewUnlockCourseScheduleId</c>).
    /// It is the only session in any enrollment the teacher is not paid for; the student's free trial never
    /// reduces teacher earnings.
    /// </param>
    public static EarningsBreakdown Compute(
        Enrollment enrollment,
        IReadOnlyList<EarningLineInfo> lines,
        decimal starterSharePct = 0m,
        int? teacherInterviewScheduleId = null)
    {
        var snap = enrollment.PricingSnapshot;
        var schedules = (enrollment.CourseSchedules ?? [])
            .Where(s => s.Status != ScheduleStatus.Cancelled && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToList();

        var interviewSchedule = teacherInterviewScheduleId.HasValue
            ? schedules.FirstOrDefault(s => s.Id == teacherInterviewScheduleId.Value)
            : null;
        var freeSessions = interviewSchedule != null ? 1 : 0;
        var paidSessions = Math.Max(0, schedules.Count - freeSessions);

        var teacherDue = snap?.TeacherEarnings ?? 0m;
        var platformCommission = snap?.PlatformShare ?? 0m;
        var sharePct = snap?.TeacherSharePct ?? 0m;

        var projection = EnrollmentEarningsProjectionHelper.Compute(enrollment, starterSharePct);
        var packageBasis = teacherDue > 0 ? teacherDue : projection?.ProjectedTeacherEarningsDue ?? 0m;

        var deduction = 0m;
        if (interviewSchedule != null && packageBasis > 0)
        {
            var totalMinutes = snap?.TotalMinutes > 0
                ? snap.TotalMinutes
                : schedules.Sum(s => s.DurationMinutes);
            deduction = TeacherEarningService.ComputeScheduleEarning(
                packageBasis, totalMinutes, interviewSchedule.DurationMinutes);
        }

        var perSession = schedules.Count > 0
            ? Math.Round(teacherDue / schedules.Count, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var accrued = lines
            .Where(l => l.Status != TeacherEarningLineStatus.Voided)
            .Sum(l => l.Amount);

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
            ProjectedFreeSessionTeacherDeduction: projection != null ? deduction : 0m,
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
