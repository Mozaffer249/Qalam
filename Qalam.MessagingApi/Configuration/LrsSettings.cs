namespace Qalam.MessagingApi.Configuration;

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
}
