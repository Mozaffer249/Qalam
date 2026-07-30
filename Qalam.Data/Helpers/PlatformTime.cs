namespace Qalam.Data.Helpers;

/// <summary>
/// Catalog TimeSlot clocks are platform-local (Arabia Standard Time / Asia/Riyadh).
/// Convert Date + wall-clock to UTC instants for lock, join, and lifecycle comparisons.
/// </summary>
public static class PlatformTime
{
    public const string IanaId = "Asia/Riyadh";
    public const string WindowsId = "Arab Standard Time";

    private static readonly Lazy<TimeZoneInfo> Tz = new(ResolveTimeZone);

    public static TimeZoneInfo TimeZone => Tz.Value;

    /// <summary>
    /// Interprets <paramref name="date"/> + <paramref name="time"/> as platform-local
    /// and returns the equivalent UTC instant.
    /// </summary>
    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var localUnspecified = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localUnspecified, TimeZone);
    }

    /// <summary>
    /// Interprets <paramref name="date"/> + <paramref name="time"/> as platform-local
    /// and returns the equivalent UTC instant.
    /// </summary>
    public static DateTime ToUtc(DateOnly date, TimeSpan time) =>
        ToUtc(date, TimeOnly.FromTimeSpan(time));

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsId);
        }
    }
}
