namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// حالة الطلب
/// </summary>
public enum RequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// حالة الجدول
/// </summary>
public enum ScheduleStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    Rescheduled = 4,
    InProgress = 5,
}

/// <summary>
/// حضور الطالب في جلسة مجدولة.
/// </summary>
public enum SessionAttendanceStatus
{
    Pending = 0,
    Present = 1,
    Late = 2,
    Absent = 3,
    Excused = 4,
}
