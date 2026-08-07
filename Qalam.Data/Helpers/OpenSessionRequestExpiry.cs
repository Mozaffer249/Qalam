namespace Qalam.Data.Helpers;

/// <summary>
/// Nested deadline model for Scenario 2 open session requests.
/// All session wall-clock values are platform-local (Asia/Riyadh) and converted via <see cref="PlatformTime"/>.
/// </summary>
public static class OpenSessionRequestExpiry
{
    /// <summary>
    /// Earliest session start as a UTC instant.
    /// Sessions with a preferred date but no time slot fall back to end of that local day.
    /// Returns null when no dated sessions exist.
    /// </summary>
    public static DateTime? FirstSessionStartUtc(
        IEnumerable<(DateOnly? Date, TimeSpan? Start)> sessions)
    {
        DateTime? earliest = null;
        foreach (var (date, start) in sessions)
        {
            if (!date.HasValue) continue;

            var localTime = start.HasValue
                ? TimeOnly.FromTimeSpan(start.Value)
                : new TimeOnly(23, 59, 59);
            var utc = PlatformTime.ToUtc(date.Value, localTime);
            if (earliest == null || utc < earliest.Value)
                earliest = utc;
        }

        return earliest;
    }

    public static int MinimumLeadHours(OpenSessionRequestSettings settings, bool isTargeted) =>
        isTargeted ? settings.TargetedMinimumLeadHours : settings.BroadcastMinimumLeadHours;

    public static int OfferCutoffHours(OpenSessionRequestSettings settings, bool isTargeted) =>
        isTargeted ? settings.TargetedOfferCutoffHours : settings.BroadcastOfferCutoffHours;

    /// <summary>
    /// <c>min(requested ?? publishedAt + RequestWindowDays, firstSessionStartUtc - OfferCutoffHours)</c>.
    /// When there is no first session, falls back to the window-only bound.
    /// </summary>
    public static DateTime ResolveRequestExpiry(
        DateTime nowUtc,
        DateTime? requested,
        DateTime? firstSessionStartUtc,
        OpenSessionRequestSettings settings,
        bool isTargeted)
    {
        var windowBound = requested ?? nowUtc.AddDays(Math.Max(1, settings.RequestWindowDays));
        if (firstSessionStartUtc == null)
            return windowBound;

        var cutoffHours = Math.Max(0, OfferCutoffHours(settings, isTargeted));
        var sessionBound = firstSessionStartUtc.Value.AddHours(-cutoffHours);
        return sessionBound < windowBound ? sessionBound : windowBound;
    }

    /// <summary>
    /// <c>min(now + ValidityHours, requestExpiresAt)</c> when request expiry is known.
    /// </summary>
    public static DateTime ResolveOfferExpiry(
        DateTime nowUtc,
        int validityHours,
        DateTime? requestExpiresAt)
    {
        var hours = Math.Max(1, validityHours);
        var candidate = nowUtc.AddHours(hours);
        if (requestExpiresAt == null)
            return candidate;
        return candidate < requestExpiresAt.Value ? candidate : requestExpiresAt.Value;
    }

    /// <summary>
    /// <c>min(acceptedAt + PaymentDeadlineHours, firstSessionStartUtc - PaymentCutoffHours)</c>.
    /// </summary>
    public static DateTime ResolvePaymentDeadline(
        DateTime acceptedAtUtc,
        int paymentDeadlineHours,
        DateTime? firstSessionStartUtc,
        OpenSessionRequestSettings settings)
    {
        var hours = Math.Max(1, paymentDeadlineHours);
        var candidate = acceptedAtUtc.AddHours(hours);
        if (firstSessionStartUtc == null)
            return candidate;

        var cutoffHours = Math.Max(0, settings.PaymentCutoffHours);
        var sessionBound = firstSessionStartUtc.Value.AddHours(-cutoffHours);
        return sessionBound < candidate ? sessionBound : candidate;
    }

    /// <summary>
    /// Effective expiry instant used for stale-transition suppression:
    /// <c>min(ExpiresAt, firstSessionStartUtc - OfferCutoffHours)</c>.
    /// </summary>
    public static DateTime EffectiveExpiryUtc(
        DateTime? expiresAt,
        DateTime? firstSessionStartUtc,
        OpenSessionRequestSettings settings,
        bool isTargeted)
    {
        DateTime? sessionBound = null;
        if (firstSessionStartUtc.HasValue)
        {
            var cutoffHours = Math.Max(0, OfferCutoffHours(settings, isTargeted));
            sessionBound = firstSessionStartUtc.Value.AddHours(-cutoffHours);
        }

        if (expiresAt.HasValue && sessionBound.HasValue)
            return expiresAt.Value < sessionBound.Value ? expiresAt.Value : sessionBound.Value;
        if (expiresAt.HasValue) return expiresAt.Value;
        if (sessionBound.HasValue) return sessionBound.Value;
        return DateTime.MinValue;
    }

    public static bool IsWithinNotificationGrace(
        DateTime effectiveInstantUtc,
        DateTime nowUtc,
        OpenSessionRequestSettings settings)
    {
        var grace = TimeSpan.FromHours(Math.Max(0, settings.NotificationGraceHours));
        return nowUtc - effectiveInstantUtc <= grace;
    }
}
