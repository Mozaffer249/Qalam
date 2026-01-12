using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class TimeSlotsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.HasAnyDataAsync(context.TimeSlots))
        {
            var timeSlots = new List<TimeSlot>();

            // Create hourly slots from 08:00 to 23:00
            for (int hour = 8; hour < 23; hour++)
            {
                var startTime = new TimeSpan(hour, 0, 0);
                var endTime = new TimeSpan(hour + 1, 0, 0);
                
                string labelAr = hour < 12 ? "فترة الصباح" : 
                                hour < 17 ? "فترة الظهيرة" : 
                                "فترة المساء";
                
                string labelEn = hour < 12 ? "Morning" : 
                                hour < 17 ? "Afternoon" : 
                                "Evening";

                timeSlots.Add(new TimeSlot
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    DurationMinutes = 60,
                    LabelAr = labelAr,
                    LabelEn = labelEn,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.TimeSlots.AddRangeAsync(timeSlots);
            await context.SaveChangesAsync();
        }
    }
}

