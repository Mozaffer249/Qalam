namespace Qalam.Data.Helpers;

/// <summary>
/// Scenario 2: settings for the offer expiry background service. Bound from
/// `OpenSessionOfferSettings` in appsettings.json.
/// </summary>
public class OpenSessionOfferSettings
{
    /// <summary>How often the background service sweeps for expired offers. Default 15 minutes.</summary>
    public int ExpirationCheckIntervalMinutes { get; set; } = 15;
}
