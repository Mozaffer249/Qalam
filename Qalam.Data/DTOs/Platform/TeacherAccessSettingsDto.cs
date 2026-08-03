using System.Text.Json;

namespace Qalam.Data.DTOs.Platform;

/// <summary>
/// Stored as JSON in common.SystemSettings (Key = Platform.TeacherAccess).
/// Controls whether activated teachers may enter the dashboard.
/// </summary>
public class TeacherAccessSettingsDto
{
    /// <summary>
    /// When false, Active teachers who finished subjects + availability get
    /// nextStep "Awaiting Platform Launch" instead of Dashboard.
    /// </summary>
    public bool TeacherDashboardReady { get; set; }
}

public static class TeacherAccessSettingsDefaults
{
    public static TeacherAccessSettingsDto Create() => new()
    {
        TeacherDashboardReady = false
    };

    public static string ToJson(TeacherAccessSettingsDto settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    public static TeacherAccessSettingsDto FromJson(string json) =>
        JsonSerializer.Deserialize<TeacherAccessSettingsDto>(json, JsonOptions) ?? Create();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
