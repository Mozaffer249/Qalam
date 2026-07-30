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

    /// <summary>
    /// Normalizes <paramref name="utcNow"/> to a UTC instant (<see cref="DateTimeKind.Utc"/>).
    /// Unspecified values are treated as UTC; local values are converted to UTC.
    /// </summary>
    public static DateTime ToUtcInstant(DateTime utcNow) =>
        utcNow.Kind switch
        {
            DateTimeKind.Utc => utcNow,
            DateTimeKind.Local => utcNow.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
        };

    /// <summary>
    /// Platform-local calendar date (Asia/Riyadh) for the given UTC instant.
    /// Use for lifecycle candidate filters so UTC midnight lag cannot skip "today".
    /// </summary>
    public static DateOnly ToPlatformDate(DateTime utcNow)
    {
        var utc = ToUtcInstant(utcNow);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
        return DateOnly.FromDateTime(local);
    }

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
