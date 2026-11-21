using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Infrastructure.Seeding;
using Qalam.Infrastructure.context;

namespace Qalam.Service.Tests;

public class TimeSlotsSeederTests
{
    // 24 hours × (1 full + 2 half) = 72 rows.
    private const int ExpectedTotal = 72;
    private const int Expected60Min = 24;
    private const int Expected30Min = 48;

    private static ApplicationDBContext CreateDb()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDBContext(options, config);
    }

    [Fact]
    public async Task SeedAsync_EmptyDb_SeedsFullAndHalfHourBandsForEntireDay()
    {
        await using var db = CreateDb();

        await TimeSlotsSeeder.SeedAsync(db);

        var all = await db.TimeSlots.ToListAsync();

        Assert.Equal(ExpectedTotal, all.Count);
        Assert.Equal(Expected60Min, all.Count(t => t.DurationMinutes == 60));
        Assert.Equal(Expected30Min, all.Count(t => t.DurationMinutes == 30));

        // 00:00 band exists (full and both halves).
        Assert.Contains(all, t =>
            t.StartTime == TimeSpan.Zero &&
            t.EndTime == new TimeSpan(1, 0, 0) &&
            t.DurationMinutes == 60 && t.IsActive);
        Assert.Contains(all, t =>
            t.StartTime == TimeSpan.Zero &&
            t.EndTime == new TimeSpan(0, 30, 0) &&
            t.DurationMinutes == 30 && t.IsActive);

        // 23:30–00:00 band uses midnight-as-00:00, which EndsNextDayFor handles.
        var lateUpper = all.Single(t =>
            t.StartTime == new TimeSpan(23, 30, 0) &&
            t.EndTime == TimeSpan.Zero);
        Assert.Equal(30, lateUpper.DurationMinutes);
        Assert.True(lateUpper.EndsNextDay);

        // The last 60-min band 23:00–00:00 also present.
        Assert.Contains(all, t =>
            t.StartTime == new TimeSpan(23, 0, 0) &&
            t.EndTime == TimeSpan.Zero &&
            t.DurationMinutes == 60);

        // Spot-check the two 30-min bands of the 09:00 hour (from the earlier plan).
        Assert.Contains(all, t =>
            t.StartTime == new TimeSpan(9, 0, 0) &&
            t.EndTime == new TimeSpan(9, 30, 0) &&
            t.DurationMinutes == 30);
        Assert.Contains(all, t =>
            t.StartTime == new TimeSpan(9, 30, 0) &&
            t.EndTime == new TimeSpan(10, 0, 0) &&
            t.DurationMinutes == 30);
    }

    [Fact]
    public async Task SeedAsync_ReRunOnAlreadySeededDb_IsIdempotent()
    {
        await using var db = CreateDb();

        await TimeSlotsSeeder.SeedAsync(db);
        await TimeSlotsSeeder.SeedAsync(db);

        Assert.Equal(ExpectedTotal, await db.TimeSlots.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ExistingLegacy60MinOnly_AddsMissingBandsOnly()
    {
        await using var db = CreateDb();

        // Simulate a legacy DB with only the original 08:00–23:00 60-min hourly rows.
        for (int hour = 8; hour < 23; hour++)
        {
            db.TimeSlots.Add(new Qalam.Data.Entity.Common.TimeSlot
            {
                StartTime = new TimeSpan(hour, 0, 0),
                EndTime = new TimeSpan(hour + 1, 0, 0),
                DurationMinutes = 60,
                LabelAr = "legacy",
                LabelEn = "legacy",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync();
        Assert.Equal(15, await db.TimeSlots.CountAsync());

        await TimeSlotsSeeder.SeedAsync(db);

        // After seeding the full day, the 15 legacy rows survive and the
        // missing 57 (24 new 30-min lower + 24 new 30-min upper + 9 new 60-min)
        // are added, bringing the catalog to 72 total rows.
        Assert.Equal(ExpectedTotal, await db.TimeSlots.CountAsync());
        Assert.Equal(15, await db.TimeSlots.CountAsync(t => t.LabelEn == "legacy"));
    }
}
