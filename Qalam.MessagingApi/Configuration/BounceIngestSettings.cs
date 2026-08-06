namespace Qalam.MessagingApi.Configuration;

public class BounceIngestSettings
{
    /// <summary>Feature flag — off by default until IMAP NDR inbox is configured.</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional folder to move processed NDRs into (e.g. "Processed").</summary>
    public string? ProcessedFolder { get; set; }

    public int PollIntervalMinutes { get; set; } = 5;
}
