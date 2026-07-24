namespace Qalam.Data.Helpers;

public class SessionSettings
{
    public int LifecycleCheckIntervalMinutes { get; set; } = 5;

    /// <summary>Minutes after scheduled end before auto-complete kicks in.</summary>
    public int GraceMinutes { get; set; } = 30;

    /// <summary>
    /// Default attendance when auto-resolving unmarked participants.
    /// Present = 1, Absent = 3 (SessionAttendanceStatus).
    /// Unjoined students default to Absent after session complete.
    /// </summary>
    public int DefaultAutoAttendanceStatus { get; set; } = 3;
}
