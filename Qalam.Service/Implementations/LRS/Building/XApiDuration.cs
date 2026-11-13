namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Formats durations as full ISO 8601 (<c>PT{HH}H{MM}M{SS}S</c>) — the only shape NELC accepts
/// (validator rule CRS004). Single source of truth so no builder hand-rolls a duration string.
/// </summary>
public static class XApiDuration
{
    public static string ToIso8601(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;

        var hours = (int)span.TotalHours; // may exceed 24 (e.g. PT30H00M00S)
        return $"PT{hours:D2}H{span.Minutes:D2}M{span.Seconds:D2}S";
    }

    public static string FromMinutes(double minutes) =>
        ToIso8601(TimeSpan.FromMinutes(minutes < 0 ? 0 : minutes));
}
