using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class EducationDomainsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await context.EducationDomains.AnyAsync())
        {
            var domains = new List<EducationDomain>
            {
                new()
                {
                    NameAr = "تعليم مدرسي",
                    NameEn = "School Education",
                    Code = "school",
                    HasCurriculum = true,
                    DescriptionAr = "التعليم المدرسي الأكاديمي بجميع مراحله",
                    DescriptionEn = "Academic school education at all levels",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "قرآن كريم",
                    NameEn = "Quran",
                    Code = "quran",
                    HasCurriculum = false,
                    DescriptionAr = "تعليم القرآن الكريم حفظاً وتلاوة وتجويداً",
                    DescriptionEn = "Quran education: memorization, recitation, and tajweed",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "لغات",
                    NameEn = "Languages",
                    Code = "language",
                    HasCurriculum = false,
                    DescriptionAr = "تعليم اللغات الأجنبية والعربية",
                    DescriptionEn = "Foreign and Arabic language education",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "مهارات عامة",
                    NameEn = "General Skills",
                    Code = "skills",
                    HasCurriculum = false,
                    DescriptionAr = "المهارات الحياتية والمهنية والتقنية",
                    DescriptionEn = "Life, professional, and technical skills",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.EducationDomains.AddRangeAsync(domains);
            await context.SaveChangesAsync();
        }
    }
}

