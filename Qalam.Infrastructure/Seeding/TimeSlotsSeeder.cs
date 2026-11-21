using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class TimeSlotsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        // Idempotent: load whatever already exists, then insert the missing
        // (StartTime, EndTime) rows so an existing DB gains the full-day
        // catalog without losing the primary-key stability of the original
        // 60-min hourly rows seeded in earlier environments.
        var existing = await context.TimeSlots
            .Select(t => new { t.StartTime, t.EndTime })
            .ToListAsync();
        var existingKeys = existing
            .Select(e => (e.StartTime, e.EndTime))
            .ToHashSet();

        var toInsert = new List<TimeSlot>();

        // Full 24 hours, 00:00 through 23:30, three bands per hour:
        //   60-min: HH:00 – (HH+1):00
        //   30-min lower: HH:00 – HH:30
        //   30-min upper: HH:30 – (HH+1):00
        // The final hour (23) wraps to 00:00, which the entity treats as
        // EndsNextDay=true (see TimeSlot.EndsNextDayFor). This avoids storing
        // the SQL-hostile 24:00:00 value and keeps every TimeSpan in [0, 24h).
        for (int hour = 0; hour < 24; hour++)
        {
            var nextHour = new TimeSpan((hour + 1) % 24, 0, 0);

            string labelAr = PeriodLabelAr(hour);
            string labelEn = PeriodLabelEn(hour);

            // 60-minute band
            TryAdd(toInsert, existingKeys,
                start: new TimeSpan(hour, 0, 0),
                end: nextHour,
                durationMinutes: 60,
                labelAr: labelAr,
                labelEn: labelEn);

            // Lower 30-minute band HH:00 – HH:30
            TryAdd(toInsert, existingKeys,
                start: new TimeSpan(hour, 0, 0),
                end: new TimeSpan(hour, 30, 0),
                durationMinutes: 30,
                labelAr: labelAr,
                labelEn: labelEn);

            // Upper 30-minute band HH:30 – (HH+1):00
            TryAdd(toInsert, existingKeys,
                start: new TimeSpan(hour, 30, 0),
                end: nextHour,
                durationMinutes: 30,
                labelAr: labelAr,
                labelEn: labelEn);
        }

        if (toInsert.Count == 0) return;

        await context.TimeSlots.AddRangeAsync(toInsert);
        await context.SaveChangesAsync();
    }

    private static string PeriodLabelAr(int hour) => hour switch
    {
        < 6 => "بعد منتصف الليل",
        < 12 => "فترة الصباح",
        < 17 => "فترة الظهيرة",
        < 22 => "فترة المساء",
        _ => "وقت متأخر",
    };

    private static string PeriodLabelEn(int hour) => hour switch
    {
        < 6 => "Late Night",
        < 12 => "Morning",
        < 17 => "Afternoon",
        < 22 => "Evening",
        _ => "Night",
    };

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
