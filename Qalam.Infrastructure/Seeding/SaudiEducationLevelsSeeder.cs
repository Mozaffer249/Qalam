using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class SaudiEducationLevelsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var saudiCurriculumId = 1; // Saudi Curriculum
        var schoolDomainId = 1; // School Education
        
        if (!await SeederHelper.HasAnyDataAsync(context.EducationLevels, el => el.CurriculumId == saudiCurriculumId))
        {
            var levels = new List<EducationLevel>
            {
                new()
                {
                    DomainId = schoolDomainId,
                    CurriculumId = saudiCurriculumId,
                    NameAr = "المرحلة الابتدائية",
                    NameEn = "Elementary",
                    OrderIndex = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    DomainId = schoolDomainId,
                    CurriculumId = saudiCurriculumId,
                    NameAr = "المرحلة المتوسطة",
                    NameEn = "Intermediate",
                    OrderIndex = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    DomainId = schoolDomainId,
                    CurriculumId = saudiCurriculumId,
                    NameAr = "المرحلة الثانوية",
                    NameEn = "Secondary",
                    OrderIndex = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.EducationLevels.AddRangeAsync(levels);
            await context.SaveChangesAsync();
        }
    }
}

