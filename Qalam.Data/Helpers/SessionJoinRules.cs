using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Helpers;

public static class SessionJoinRules
{
    /// <summary>
    /// Join is allowed while the enrollment is Active and the schedule is Scheduled or InProgress.
    /// When <paramref name="enforceJoinWindow"/> is true, also requires the current UTC time
    /// to fall inside the scheduled start/end window.
    /// </summary>
    public static bool CanJoinUtc(
        EnrollmentStatus? enrollmentStatus,
        ScheduleStatus status,
        DateOnly date,
        TimeSpan? startTime,
        TimeSpan? endTime,
        DateTime utcNow,
        bool enforceJoinWindow = true)
    {
        if (enrollmentStatus != EnrollmentStatus.Active) return false;
        if (status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress)) return false;
        if (startTime == null || endTime == null) return false;

        if (endTime.Value == startTime.Value) return false;

        if (!enforceJoinWindow)
            return true;

        var endDate = TimeSlot.EndsNextDayFor(startTime.Value, endTime.Value) ? date.AddDays(1) : date;
        var startUtc = PlatformTime.ToUtc(date, startTime.Value);
        var endUtc = PlatformTime.ToUtc(endDate, endTime.Value);
        return utcNow >= startUtc && utcNow <= endUtc;
    }
}
