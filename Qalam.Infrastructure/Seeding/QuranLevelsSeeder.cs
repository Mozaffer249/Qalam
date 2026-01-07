using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class QuranLevelsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await context.QuranLevels.AnyAsync())
        {
            var quranLevels = new List<QuranLevel>
            {
                new()
                {
                    NameAr = "تمهيدي",
                    NameEn = "Preparatory",
                    OrderIndex = 1,
                    DescriptionAr = "مستوى تمهيدي لتعلم القرآن الكريم",
                    DescriptionEn = "Preparatory level for Quran learning",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "مبتدئ",
                    NameEn = "Beginner",
                    OrderIndex = 2,
                    DescriptionAr = "مستوى مبتدئ لتعلم القرآن الكريم",
                    DescriptionEn = "Beginner level for Quran learning",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "متوسط",
                    NameEn = "Intermediate",
                    OrderIndex = 3,
                    DescriptionAr = "مستوى متوسط لتعلم القرآن الكريم",
                    DescriptionEn = "Intermediate level for Quran learning",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "متقدم",
                    NameEn = "Advanced",
                    OrderIndex = 4,
                    DescriptionAr = "مستوى متقدم لتعلم القرآن الكريم",
                    DescriptionEn = "Advanced level for Quran learning",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.QuranLevels.AddRangeAsync(quranLevels);
            await context.SaveChangesAsync();
        }
    }
}

