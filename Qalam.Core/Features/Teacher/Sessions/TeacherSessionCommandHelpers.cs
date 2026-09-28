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

        if (timeSlot.EndTime == timeSlot.StartTime)
            return false;

        if (!enforceJoinWindow)
            return true;

        var startUtc = PlatformTime.ToUtc(schedule.Date, timeSlot.StartTime);
        var endUtc = PlatformTime.ToUtc(timeSlot.GetEndDate(schedule.Date), timeSlot.EndTime);
        return utcNow >= startUtc && utcNow <= endUtc;
    }
}
