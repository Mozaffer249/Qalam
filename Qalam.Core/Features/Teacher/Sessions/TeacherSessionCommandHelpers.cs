using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

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

    public static bool CanStartSessionUtc(CourseSchedule schedule, DateTime utcNow)
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

        var startUtc = schedule.Date.ToDateTime(start, DateTimeKind.Utc);
        var endUtc = schedule.Date.ToDateTime(end, DateTimeKind.Utc);
        return utcNow >= startUtc && utcNow <= endUtc;
    }
}
