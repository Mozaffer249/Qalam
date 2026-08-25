using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Helpers;

/// <summary>
/// Shared gates for student cancel-before-first-session and enrollment auto-complete.
/// </summary>
public static class EnrollmentLifecycleRules
{
    /// <summary>
    /// True when any schedule has started (InProgress/Completed) or has non-pending attendance.
    /// </summary>
    public static bool HasSessionStarted(Enrollment enrollment)
    {
        var schedules = enrollment.CourseSchedules;
        if (schedules == null || schedules.Count == 0)
            return false;

        foreach (var schedule in schedules)
        {
            if (schedule.Status is ScheduleStatus.InProgress or ScheduleStatus.Completed)
                return true;

            if (schedule.TeacherAttendanceStatus is SessionAttendanceStatus.Present
                or SessionAttendanceStatus.Late
                or SessionAttendanceStatus.Absent
                or SessionAttendanceStatus.Excused)
                return true;

            if (schedule.Attendances != null
                && schedule.Attendances.Any(a => a.Status is SessionAttendanceStatus.Present
                    or SessionAttendanceStatus.Late
                    or SessionAttendanceStatus.Absent
                    or SessionAttendanceStatus.Excused))
                return true;
        }

        return false;
    }

    public static bool CanStudentCancel(Enrollment enrollment, bool isOwner)
    {
        if (!isOwner)
            return false;

        if (enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment)
            return true;

        if (enrollment.EnrollmentStatus == EnrollmentStatus.Active)
            return !HasSessionStarted(enrollment);

        return false;
    }

    /// <summary>
    /// Active enrollment with ≥1 Completed schedule and no remaining Scheduled/InProgress.
    /// Cancelled/Rescheduled do not block completion.
    /// </summary>
    public static bool ShouldMarkEnrollmentCompleted(Enrollment enrollment)
    {
        if (enrollment.EnrollmentStatus is EnrollmentStatus.Completed or EnrollmentStatus.Cancelled)
            return false;

        if (enrollment.EnrollmentStatus != EnrollmentStatus.Active)
            return false;

        var schedules = enrollment.CourseSchedules;
        if (schedules == null || schedules.Count == 0)
            return false;

        var hasCompleted = false;
        foreach (var schedule in schedules)
        {
            if (schedule.Status is ScheduleStatus.Scheduled or ScheduleStatus.InProgress)
                return false;
            if (schedule.Status == ScheduleStatus.Completed)
                hasCompleted = true;
        }

        return hasCompleted;
    }
}
