namespace Qalam.Data.DTOs.Live;

/// <summary>
/// Provider-neutral credentials for joining a live session room.
/// Clients branch on <see cref="Provider"/> when connecting the RTC SDK.
/// </summary>
public class LiveSessionAccessDto
{
    public string Provider { get; set; } = string.Empty;

    /// <summary>RTC server URL (e.g. LiveKit wss://…).</summary>
    public string ServerUrl { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string Identity { get; set; } = string.Empty;

    /// <summary>teacher | student</summary>
    public string Role { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}

public class LiveSessionAccessRequest
{
    public string RoomName { get; set; } = string.Empty;

    public string Identity { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>teacher | student</summary>
    public string Role { get; set; } = string.Empty;

    public TimeSpan Ttl { get; set; }
}
