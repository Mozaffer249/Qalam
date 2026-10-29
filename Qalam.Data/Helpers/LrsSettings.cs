namespace Qalam.Data.Helpers;

public class LrsSettings
{
    public const string SectionName = "LrsSettings";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public string PlatformIri { get; set; } = "https://qalam.net.sa";
    public string XApiVersion { get; set; } = "1.0.3";

    /// <summary>NELC-assigned platform key (e.g. "QLAM-001"). Used in context.platform.</summary>
    public string PlatformKey { get; set; } = string.Empty;

    /// <summary>Platform name in Arabic for NELC extensions.</summary>
    public string PlatformNameAr { get; set; } = "قلم";

    /// <summary>Platform name in English for NELC extensions.</summary>
    public string PlatformNameEn { get; set; } = "Qalam";
}
