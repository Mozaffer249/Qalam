using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Helpers;

public static class SessionDisplayStatusHelper
{
    public static (string Primary, List<string> Hints) Compute(
        ScheduleStatus scheduleStatus,
        SessionAttendanceStatus teacherStatus,
        SessionAttendanceStatus? studentStatus)
    {
        var hints = new List<string>();

        if (scheduleStatus == ScheduleStatus.Cancelled)
            return ("Cancelled", hints);

        if (scheduleStatus == ScheduleStatus.Scheduled)
            return ("Scheduled", hints);

        if (scheduleStatus == ScheduleStatus.InProgress)
            return ("InProgress", hints);

        if (scheduleStatus == ScheduleStatus.Completed)
            hints.Add("Completed");

        if (teacherStatus is SessionAttendanceStatus.Present or SessionAttendanceStatus.Late)
            hints.Add("TeacherPresent");
        else if (teacherStatus == SessionAttendanceStatus.Absent)
            hints.Add("TeacherAbsent");

        if (studentStatus is SessionAttendanceStatus.Present or SessionAttendanceStatus.Late)
            hints.Add("StudentPresent");
        else if (studentStatus == SessionAttendanceStatus.Absent)
            hints.Add("StudentAbsent");

        var primary = scheduleStatus switch
        {
            ScheduleStatus.Completed => "Completed",
            ScheduleStatus.InProgress => "InProgress",
            ScheduleStatus.Scheduled => "Scheduled",
            _ => scheduleStatus.ToString(),
        };

        return (primary, hints);
    }

    public static bool CanStudentFileComplaint(
        ScheduleStatus scheduleStatus,
        SessionAttendanceStatus teacherStatus,
        bool hasOpenComplaint) =>
        !hasOpenComplaint && SessionComplaintRules.CanStudentFileComplaint(scheduleStatus, teacherStatus);
}
