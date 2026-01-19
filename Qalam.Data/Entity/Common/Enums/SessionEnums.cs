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
    Rescheduled = 4
}
