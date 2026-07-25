namespace Qalam.Data.Helpers;

/// <summary>
/// Live A/V provider settings. Bound from environment (.env → LiveSession__*),
/// not from appsettings. Swap vendors by implementing ILiveSessionProvider
/// and setting LIVE_SESSION_PROVIDER / LiveSession__Provider.
/// </summary>
public class LiveSessionSettings
{
    public const string SectionName = "LiveSession";

    /// <summary>Active provider key. Currently supported: LiveKit.</summary>
    public string Provider { get; set; } = "LiveKit";

    public LiveKitProviderSettings LiveKit { get; set; } = new();
}

public class LiveKitProviderSettings
{
    /// <summary>WebSocket URL, e.g. wss://your-project.livekit.cloud</summary>
    public string Url { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public int TokenTtlMinutes { get; set; } = 120;
}
