using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Helpers;

public static class SessionJoinRules
{
    /// <summary>
    /// Join is allowed only inside the scheduled window (not before start, not after end)
    /// while the enrollment is Active and the schedule is Scheduled or InProgress.
    /// </summary>
    public static bool CanJoinUtc(
        EnrollmentStatus? enrollmentStatus,
        ScheduleStatus status,
        DateOnly date,
        TimeSpan? startTime,
        TimeSpan? endTime,
        DateTime utcNow)
    {
        if (enrollmentStatus != EnrollmentStatus.Active) return false;
        if (status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress)) return false;
        if (startTime == null || endTime == null) return false;

        var start = TimeOnly.FromTimeSpan(startTime.Value);
        var end = TimeOnly.FromTimeSpan(endTime.Value);
        if (end <= start) return false;

        var startUtc = date.ToDateTime(start, DateTimeKind.Utc);
        var endUtc = date.ToDateTime(end, DateTimeKind.Utc);
        return utcNow >= startUtc && utcNow <= endUtc;
    }
}
