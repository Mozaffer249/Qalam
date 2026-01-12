using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class LanguageLevelsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var languageDomainId = 3; // Languages domain

        if (!await SeederHelper.HasAnyDataAsync(context.EducationLevels, el => el.DomainId == languageDomainId))
        {
            var levels = new List<EducationLevel>
            {
                new()
                {
                    DomainId = languageDomainId,
                    NameAr = "مستوى مبتدئ",
                    NameEn = "Beginner Level",
                    OrderIndex = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    DomainId = languageDomainId,
                    NameAr = "مستوى متوسط",
                    NameEn = "Intermediate Level",
                    OrderIndex = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    DomainId = languageDomainId,
                    NameAr = "مستوى متقدم",
                    NameEn = "Advanced Level",
                    OrderIndex = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.EducationLevels.AddRangeAsync(levels);
            await context.SaveChangesAsync();
        }

        // Now seed grades (proficiency levels) for each language level
        var beginnerLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.DomainId == languageDomainId && el.NameEn == "Beginner Level");
        var intermediateLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.DomainId == languageDomainId && el.NameEn == "Intermediate Level");
        var advancedLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.DomainId == languageDomainId && el.NameEn == "Advanced Level");

        if (beginnerLevel == null || intermediateLevel == null || advancedLevel == null)
        {
            throw new Exception("Language levels must be created before grades");
        }

        if (!await SeederHelper.HasAnyDataAsync(context.Grades, g => g.LevelId == beginnerLevel.Id))
        {
            var grades = new List<Grade>
            {
                // Beginner proficiency levels
                new() { LevelId = beginnerLevel.Id, NameAr = "A1 - مبتدئ أساسي", NameEn = "A1 - Basic Beginner", OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = beginnerLevel.Id, NameAr = "A2 - مبتدئ متقدم", NameEn = "A2 - Elementary", OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                
                // Intermediate proficiency levels
                new() { LevelId = intermediateLevel.Id, NameAr = "B1 - متوسط أساسي", NameEn = "B1 - Intermediate", OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = intermediateLevel.Id, NameAr = "B2 - متوسط متقدم", NameEn = "B2 - Upper Intermediate", OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                
                // Advanced proficiency levels
                new() { LevelId = advancedLevel.Id, NameAr = "C1 - متقدم", NameEn = "C1 - Advanced", OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = advancedLevel.Id, NameAr = "C2 - إتقان", NameEn = "C2 - Proficiency", OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await context.Grades.AddRangeAsync(grades);
            await context.SaveChangesAsync();
        }
    }
}

