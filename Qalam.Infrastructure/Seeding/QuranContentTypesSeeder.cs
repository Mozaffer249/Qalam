using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class QuranContentTypesSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await context.QuranContentTypes.AnyAsync())
        {
            var contentTypes = new List<QuranContentType>
            {
                new()
                {
                    NameAr = "حفظ",
                    NameEn = "Memorization",
                    Code = "memorization",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "تلاوة",
                    NameEn = "Recitation",
                    Code = "recitation",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "تجويد",
                    NameEn = "Tajweed",
                    Code = "tajweed",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.QuranContentTypes.AddRangeAsync(contentTypes);
            await context.SaveChangesAsync();
        }
    }
}

