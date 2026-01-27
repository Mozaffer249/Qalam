using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class DayOfWeekSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.HasAnyDataAsync(context.DaysOfWeek))
        {
            var daysOfWeek = new List<DayOfWeekMaster>
            {
                new()
                {
                    Id = 1,
                    NameAr = "الأحد",
                    NameEn = "Sunday",
                    OrderIndex = 1,
                    IsActive = true
                },
                new()
                {
                    Id = 2,
                    NameAr = "الإثنين",
                    NameEn = "Monday",
                    OrderIndex = 2,
                    IsActive = true
                },
                new()
                {
                    Id = 3,
                    NameAr = "الثلاثاء",
                    NameEn = "Tuesday",
                    OrderIndex = 3,
                    IsActive = true
                },
                new()
                {
                    Id = 4,
                    NameAr = "الأربعاء",
                    NameEn = "Wednesday",
                    OrderIndex = 4,
                    IsActive = true
                },
                new()
                {
                    Id = 5,
                    NameAr = "الخميس",
                    NameEn = "Thursday",
                    OrderIndex = 5,
                    IsActive = true
                },
                new()
                {
                    Id = 6,
                    NameAr = "الجمعة",
                    NameEn = "Friday",
                    OrderIndex = 6,
                    IsActive = true
                },
                new()
                {
                    Id = 7,
                    NameAr = "السبت",
                    NameEn = "Saturday",
                    OrderIndex = 7,
                    IsActive = true
                }
            };

            await context.DaysOfWeek.AddRangeAsync(daysOfWeek);
            await context.SaveChangesAsync();
        }
    }
}
