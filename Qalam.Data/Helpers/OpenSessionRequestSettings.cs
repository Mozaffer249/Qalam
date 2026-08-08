namespace Qalam.Data.Helpers;

/// <summary>
/// Scenario 2: open session request lifecycle settings.
/// Bound from <c>OpenSessionRequestSettings</c> in appsettings.json.
/// </summary>
public class OpenSessionRequestSettings
{
    /// <summary>Max window from publish before the request expires, regardless of session date.</summary>
    public int RequestWindowDays { get; set; } = 7;

    /// <summary>Broadcast: first session must start at least this many hours from now.</summary>
    public int BroadcastMinimumLeadHours { get; set; } = 3;

    /// <summary>Broadcast: stop accepting offers this many hours before the first session.</summary>
    public int BroadcastOfferCutoffHours { get; set; } = 2;

    /// <summary>Broadcast: payment deadline must end at least this many hours before the first session.</summary>
    public int BroadcastPaymentCutoffHours { get; set; } = 1;

    /// <summary>Targeted: first session must start at least this many hours from now.</summary>
    public int TargetedMinimumLeadHours { get; set; } = 2;

    /// <summary>Targeted: stop accepting offers this many hours before the first session.</summary>
    public int TargetedOfferCutoffHours { get; set; } = 1;

    /// <summary>Targeted: payment deadline must end at least this many hours before the first session (0 = pay up to session start).</summary>
    public int TargetedPaymentCutoffHours { get; set; } = 0;

    /// <summary>Default offer validity when the teacher does not specify one.</summary>
    public int DefaultOfferValidityHours { get; set; } = 48;

    /// <summary>How often the lifecycle background service sweeps.</summary>
    public int SweepIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Hours before <c>ExpiresAt</c> at which to send expiry-soon nudges (descending).
    /// Stage N uses index N-1.
    /// </summary>
    public int[] ExpiryNudgeHours { get; set; } = [24, 6];

    /// <summary>
    /// Only email on lifecycle transitions whose effective instant is within this many hours of now.
    /// Suppresses the first-tick backlog flood after deploy.
    /// </summary>
    public int NotificationGraceHours { get; set; } = 6;
}
