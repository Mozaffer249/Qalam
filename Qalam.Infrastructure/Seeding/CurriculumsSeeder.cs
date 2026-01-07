using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class CurriculumsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await context.Curriculums.AnyAsync())
        {
            var curriculums = new List<Curriculum>
            {
                new()
                {
                    NameAr = "المنهج السعودي",
                    NameEn = "Saudi Curriculum",
                    Country = "Saudi Arabia",
                    DescriptionAr = "المنهج التعليمي المعتمد في المملكة العربية السعودية",
                    DescriptionEn = "The official curriculum adopted in Saudi Arabia",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "المنهج المصري",
                    NameEn = "Egyptian Curriculum",
                    Country = "Egypt",
                    DescriptionAr = "المنهج التعليمي المعتمد في جمهورية مصر العربية",
                    DescriptionEn = "The official curriculum adopted in Egypt",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "المنهج الأمريكي",
                    NameEn = "American Curriculum",
                    Country = "United States",
                    DescriptionAr = "المنهج التعليمي الأمريكي المعتمد",
                    DescriptionEn = "The official American curriculum",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Curriculums.AddRangeAsync(curriculums);
            await context.SaveChangesAsync();
        }
    }
}

