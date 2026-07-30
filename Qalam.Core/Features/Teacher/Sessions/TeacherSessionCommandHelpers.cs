using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;

namespace Qalam.Core.Features.Teacher.Sessions;

internal static class TeacherSessionCommandHelpers
{
    public static bool TeacherOwnsSchedule(CourseSchedule schedule, int teacherId)
    {
        if (schedule.Enrollment == null)
            return false;

        if (schedule.Enrollment.ApprovedByTeacherId == teacherId)
            return true;

        return schedule.Enrollment.Course != null
               && schedule.Enrollment.Course.TeacherId == teacherId;
    }

    public static bool CanStartSessionUtc(
        CourseSchedule schedule,
        DateTime utcNow,
        bool enforceJoinWindow = true)
    {
        if (schedule.Enrollment?.EnrollmentStatus != EnrollmentStatus.Active)
            return false;

        if (schedule.Status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress))
            return false;

        var timeSlot = schedule.TeacherAvailability?.TimeSlot;
        if (timeSlot == null)
            return false;

        var start = TimeOnly.FromTimeSpan(timeSlot.StartTime);
        var end = TimeOnly.FromTimeSpan(timeSlot.EndTime);
        if (end <= start)
            return false;

        if (!enforceJoinWindow)
            return true;

        var startUtc = PlatformTime.ToUtc(schedule.Date, start);
        var endUtc = PlatformTime.ToUtc(schedule.Date, end);
        return utcNow >= startUtc && utcNow <= endUtc;
    }
}
