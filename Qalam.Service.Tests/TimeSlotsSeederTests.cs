using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Infrastructure.Seeding;
using Qalam.Infrastructure.context;

namespace Qalam.Service.Tests;

public class TimeSlotsSeederTests
{
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
    public async Task SeedAsync_EmptyDb_SeedsFullAndHalfHourBands()
    {
        await using var db = CreateDb();

        await TimeSlotsSeeder.SeedAsync(db);

        var all = await db.TimeSlots.ToListAsync();

        // 15 hours (08..22) × (1 full + 2 half) = 45 rows.
        Assert.Equal(45, all.Count);
        Assert.Equal(15, all.Count(t => t.DurationMinutes == 60));
        Assert.Equal(30, all.Count(t => t.DurationMinutes == 30));

        // Spot-check the two 30-min bands of the 09:00 hour.
        Assert.Contains(all, t =>
            t.StartTime == new TimeSpan(9, 0, 0) &&
            t.EndTime == new TimeSpan(9, 30, 0) &&
            t.DurationMinutes == 30 && t.IsActive);
        Assert.Contains(all, t =>
            t.StartTime == new TimeSpan(9, 30, 0) &&
            t.EndTime == new TimeSpan(10, 0, 0) &&
            t.DurationMinutes == 30 && t.IsActive);

        // 60-min band for the same hour still present (unchanged).
        Assert.Contains(all, t =>
            t.StartTime == new TimeSpan(9, 0, 0) &&
            t.EndTime == new TimeSpan(10, 0, 0) &&
            t.DurationMinutes == 60);
    }

    [Fact]
    public async Task SeedAsync_ReRunOnAlreadySeededDb_IsIdempotent()
    {
        await using var db = CreateDb();

        await TimeSlotsSeeder.SeedAsync(db);
        await TimeSlotsSeeder.SeedAsync(db);

        Assert.Equal(45, await db.TimeSlots.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ExistingLegacy60MinOnly_AddsMissingHalfHourBandsOnly()
    {
        await using var db = CreateDb();

        // Simulate a legacy DB with only the original 60-min hourly bands.
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

        var countBefore = await db.TimeSlots.CountAsync();
        Assert.Equal(15, countBefore);

        await TimeSlotsSeeder.SeedAsync(db);

        // 30 half-hour rows added; 15 legacy rows preserved (same IDs).
        Assert.Equal(45, await db.TimeSlots.CountAsync());
        Assert.Equal(15, await db.TimeSlots.CountAsync(t => t.LabelEn == "legacy"));
    }
}
