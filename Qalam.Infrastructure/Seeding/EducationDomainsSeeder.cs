using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class EducationDomainsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        // Safely check if data exists (returns false if table doesn't exist)
        if (!await SeederHelper.HasAnyDataAsync(context.EducationDomains))
        {
            var domains = new List<EducationDomain>
            {
                new()
                {
                    NameAr = "تعليم مدرسي",
                    NameEn = "School Education",
                    Code = "school",
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
                    DescriptionAr = "المهارات الحياتية والمهنية والتقنية",
                    DescriptionEn = "Life, professional, and technical skills",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    NameAr = "تعليم جامعي",
                    NameEn = "University Education",
                    Code = "university",
                    DescriptionAr = "التعليم الجامعي والدراسات العليا",
                    DescriptionEn = "University and higher education",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.EducationDomains.AddRangeAsync(domains);
            await context.SaveChangesAsync();
        }
    }
}

