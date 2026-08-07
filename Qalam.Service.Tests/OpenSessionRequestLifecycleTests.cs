using Qalam.Data.Helpers;
using Xunit;

namespace Qalam.Service.Tests;

public class OpenSessionRequestLifecycleTests
{
    private static OpenSessionRequestSettings Settings() => new()
    {
        RequestWindowDays = 7,
        BroadcastMinimumLeadHours = 24,
        BroadcastOfferCutoffHours = 12,
        TargetedMinimumLeadHours = 6,
        TargetedOfferCutoffHours = 3,
        PaymentCutoffHours = 2,
        NotificationGraceHours = 6,
    };

    [Fact]
    public void ResolveRequestExpiry_TakesSessionBound_WhenEarlierThanWindow()
    {
        var settings = Settings();
        // Session at 10:00 Riyadh on 2026-08-10 = 07:00 UTC
        var firstStart = PlatformTime.ToUtc(new DateOnly(2026, 8, 10), new TimeOnly(10, 0));
        var published = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);

        var expiry = OpenSessionRequestExpiry.ResolveRequestExpiry(
            published, null, firstStart, settings, isTargeted: false);

        // 07:00 UTC - 12h = 2026-08-09 19:00 UTC
        Assert.Equal(new DateTime(2026, 8, 9, 19, 0, 0, DateTimeKind.Utc), expiry);
    }

    [Fact]
    public void ResolveRequestExpiry_TakesWindow_WhenSessionIsFarAway()
    {
        var settings = Settings();
        var published = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstStart = published.AddDays(30);

        var expiry = OpenSessionRequestExpiry.ResolveRequestExpiry(
            published, null, firstStart, settings, isTargeted: false);

        Assert.Equal(published.AddDays(7), expiry);
    }

    [Fact]
    public void ResolveOfferExpiry_NeverExceedsRequestExpiry()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc);
        var requestExpiry = now.AddHours(10);

        var offerExpiry = OpenSessionRequestExpiry.ResolveOfferExpiry(now, 48, requestExpiry);

        Assert.Equal(requestExpiry, offerExpiry);
    }

    [Fact]
    public void ResolvePaymentDeadline_ClampsToSessionMinusPaymentCutoff()
    {
        var settings = Settings();
        var accepted = new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc);
        var firstStart = accepted.AddHours(8); // session in 8h

        var deadline = OpenSessionRequestExpiry.ResolvePaymentDeadline(
            accepted, paymentDeadlineHours: 48, firstStart, settings);

        // firstStart - 2h = accepted + 6h
        Assert.Equal(firstStart.AddHours(-2), deadline);
    }

    [Fact]
    public void MinimumLead_DiffersForTargetedAndBroadcast()
    {
        var settings = Settings();
        Assert.Equal(24, OpenSessionRequestExpiry.MinimumLeadHours(settings, isTargeted: false));
        Assert.Equal(6, OpenSessionRequestExpiry.MinimumLeadHours(settings, isTargeted: true));
    }

    [Fact]
    public void EffectiveExpiry_UsesSessionBound_WhenExpiresAtIsInTheFuture()
    {
        // Reproduces request #5: ExpiresAt days away, but session yesterday.
        var settings = Settings();
        var expiresAt = new DateTime(2026, 8, 13, 0, 43, 0, DateTimeKind.Utc);
        var firstStart = PlatformTime.ToUtc(new DateOnly(2026, 8, 6), new TimeOnly(9, 0));
        // 09:00 Riyadh Aug 6 = 06:00 UTC; minus 12h = Aug 5 18:00 UTC

        var effective = OpenSessionRequestExpiry.EffectiveExpiryUtc(
            expiresAt, firstStart, settings, isTargeted: false);

        Assert.True(effective < expiresAt);
        Assert.Equal(firstStart.AddHours(-12), effective);
    }

    [Fact]
    public void NotificationGrace_SuppressesStaleTransitions()
    {
        var settings = Settings();
        var now = new DateTime(2026, 8, 7, 3, 0, 0, DateTimeKind.Utc);
        var stale = now.AddHours(-48);
        var fresh = now.AddHours(-1);

        Assert.False(OpenSessionRequestExpiry.IsWithinNotificationGrace(stale, now, settings));
        Assert.True(OpenSessionRequestExpiry.IsWithinNotificationGrace(fresh, now, settings));
    }

    [Fact]
    public void FirstSessionStartUtc_FallsBackToEndOfDay_WhenNoTimeSlot()
    {
        var start = OpenSessionRequestExpiry.FirstSessionStartUtc(
            new (DateOnly?, TimeSpan?)[]
            {
                (new DateOnly(2026, 8, 10), null),
            });

        Assert.NotNull(start);
        var expected = PlatformTime.ToUtc(new DateOnly(2026, 8, 10), new TimeOnly(23, 59, 59));
        Assert.Equal(expected, start);
    }
}
