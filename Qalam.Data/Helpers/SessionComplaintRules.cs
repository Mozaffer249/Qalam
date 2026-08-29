using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Helpers;

public static class SessionComplaintRules
{
    public static bool IsBlockingStatus(SessionComplaintStatus status) =>
        status is SessionComplaintStatus.Open
            or SessionComplaintStatus.InReview
            or SessionComplaintStatus.AwaitingTeacher
            or SessionComplaintStatus.AwaitingStudent;

    public static bool CanStudentFileComplaint(ScheduleStatus scheduleStatus, SessionAttendanceStatus? teacherStatus) =>
        scheduleStatus is ScheduleStatus.Completed or ScheduleStatus.InProgress or ScheduleStatus.Scheduled
        && (teacherStatus is SessionAttendanceStatus.Absent or SessionAttendanceStatus.Pending
            || scheduleStatus == ScheduleStatus.Completed);
}
