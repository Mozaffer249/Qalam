using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class TimeSlotsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        // Idempotent: load whatever already exists, then insert the missing
        // (StartTime, EndTime) rows so an existing DB gains the 30-min bands
        // without losing the primary-key stability of the original 60-min rows.
        var existing = await context.TimeSlots
            .Select(t => new { t.StartTime, t.EndTime })
            .ToListAsync();
        var existingKeys = existing
            .Select(e => (e.StartTime, e.EndTime))
            .ToHashSet();

        var toInsert = new List<TimeSlot>();

        for (int hour = 8; hour < 23; hour++)
        {
            string labelAr = hour < 12 ? "فترة الصباح" :
                             hour < 17 ? "فترة الظهيرة" :
                             "فترة المساء";
            string labelEn = hour < 12 ? "Morning" :
                             hour < 17 ? "Afternoon" :
                             "Evening";

            // Full 60-minute band (original seed — keeps existing IDs stable).
            TryAdd(toInsert, existingKeys,
                start: new TimeSpan(hour, 0, 0),
                end: new TimeSpan(hour + 1, 0, 0),
                durationMinutes: 60,
                labelAr: labelAr,
                labelEn: labelEn);

            // Lower half-hour band HH:00 – HH:30
            TryAdd(toInsert, existingKeys,
                start: new TimeSpan(hour, 0, 0),
                end: new TimeSpan(hour, 30, 0),
                durationMinutes: 30,
                labelAr: labelAr,
                labelEn: labelEn);

            // Upper half-hour band HH:30 – (HH+1):00
            TryAdd(toInsert, existingKeys,
                start: new TimeSpan(hour, 30, 0),
                end: new TimeSpan(hour + 1, 0, 0),
                durationMinutes: 30,
                labelAr: labelAr,
                labelEn: labelEn);
        }

        if (toInsert.Count == 0) return;

        await context.TimeSlots.AddRangeAsync(toInsert);
        await context.SaveChangesAsync();
    }

    private static void TryAdd(
        List<TimeSlot> target,
        HashSet<(TimeSpan Start, TimeSpan End)> existingKeys,
        TimeSpan start,
        TimeSpan end,
        int durationMinutes,
        string labelAr,
        string labelEn)
    {
        if (!existingKeys.Add((start, end))) return;
        target.Add(new TimeSlot
        {
            StartTime = start,
            EndTime = end,
            DurationMinutes = durationMinutes,
            LabelAr = labelAr,
            LabelEn = labelEn,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
    }
}
