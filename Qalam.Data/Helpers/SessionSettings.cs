namespace Qalam.Data.Helpers;

public class SessionSettings
{
    public int LifecycleCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Reserved / unused for end-time auto-complete (completes at scheduled end).
    /// Kept for config compatibility; prefer 0.
    /// </summary>
    public int GraceMinutes { get; set; } = 0;

    /// <summary>
    /// Default attendance when auto-resolving unmarked participants on complete.
    /// Must be Absent (3). Present (1) is ignored — never invent Present for never-joined.
    /// </summary>
    public int DefaultAutoAttendanceStatus { get; set; } = 3;

    /// <summary>
    /// Minutes after scheduled start within which a join still counts as Present.
    /// Join after start + this grace → Late.
    /// </summary>
    public int LateGraceMinutes { get; set; } = 10;

    /// <summary>
    /// When true, Join/Start require the scheduled UTC window.
    /// When false (typically local/dev via SESSION_ENFORCE_JOIN_WINDOW), time is ignored.
    /// </summary>
    public bool EnforceJoinWindow { get; set; } = true;
}
