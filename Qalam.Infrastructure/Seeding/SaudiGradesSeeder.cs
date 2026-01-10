using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class SaudiGradesSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var saudiCurriculumId = 1;
        
        // Get education levels for Saudi curriculum
        var elementaryLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.CurriculumId == saudiCurriculumId && el.NameEn == "Elementary");
        var intermediateLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.CurriculumId == saudiCurriculumId && el.NameEn == "Intermediate");
        var secondaryLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.CurriculumId == saudiCurriculumId && el.NameEn == "Secondary");

        if (elementaryLevel == null || intermediateLevel == null || secondaryLevel == null)
        {
            throw new Exception("Saudi education levels must be seeded before grades");
        }

        if (!await context.Grades.AnyAsync(g => g.LevelId == elementaryLevel.Id))
        {
            var grades = new List<Grade>
            {
                // Elementary Grades (1-6)
                new() { LevelId = elementaryLevel.Id, NameAr = "الصف الأول الابتدائي", NameEn = "First Grade", OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = elementaryLevel.Id, NameAr = "الصف الثاني الابتدائي", NameEn = "Second Grade", OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = elementaryLevel.Id, NameAr = "الصف الثالث الابتدائي", NameEn = "Third Grade", OrderIndex = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = elementaryLevel.Id, NameAr = "الصف الرابع الابتدائي", NameEn = "Fourth Grade", OrderIndex = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = elementaryLevel.Id, NameAr = "الصف الخامس الابتدائي", NameEn = "Fifth Grade", OrderIndex = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = elementaryLevel.Id, NameAr = "الصف السادس الابتدائي", NameEn = "Sixth Grade", OrderIndex = 6, IsActive = true, CreatedAt = DateTime.UtcNow },
                
                // Intermediate Grades (1-3)
                new() { LevelId = intermediateLevel.Id, NameAr = "الصف الأول المتوسط", NameEn = "First Grade", OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = intermediateLevel.Id, NameAr = "الصف الثاني المتوسط", NameEn = "Second Grade", OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = intermediateLevel.Id, NameAr = "الصف الثالث المتوسط", NameEn = "Third Grade", OrderIndex = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                
                // Secondary Grades (1-3)
                new() { LevelId = secondaryLevel.Id, NameAr = "الصف الأول الثانوي", NameEn = "First Grade", OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = secondaryLevel.Id, NameAr = "الصف الثاني الثانوي", NameEn = "Second Grade", OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { LevelId = secondaryLevel.Id, NameAr = "الصف الثالث الثانوي", NameEn = "Third Grade", OrderIndex = 3, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await context.Grades.AddRangeAsync(grades);
            await context.SaveChangesAsync();
        }
    }
}

