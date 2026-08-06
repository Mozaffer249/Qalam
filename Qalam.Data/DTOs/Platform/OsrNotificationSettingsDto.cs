using System.Text.Json;

namespace Qalam.Data.DTOs.Platform;

/// <summary>
/// Stored as JSON in common.SystemSettings (Key = OSR.Notifications).
/// Controls outbound channels when teachers are matched/targeted on open session requests.
/// </summary>
public class OsrNotificationSettingsDto
{
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
}

public static class OsrNotificationSettingsDefaults
{
    public static OsrNotificationSettingsDto Create() => new()
    {
        EmailEnabled = true,
        SmsEnabled = false,
        PushEnabled = false
    };

    public static string ToJson(OsrNotificationSettingsDto settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    public static OsrNotificationSettingsDto FromJson(string json) =>
        JsonSerializer.Deserialize<OsrNotificationSettingsDto>(json, JsonOptions) ?? Create();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
