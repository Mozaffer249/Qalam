namespace Qalam.MessagingApi.Models.Entities;

/// <summary>Read-only mapping to common.SystemSettings (shared with main API).</summary>
public class SystemSettingRow
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
