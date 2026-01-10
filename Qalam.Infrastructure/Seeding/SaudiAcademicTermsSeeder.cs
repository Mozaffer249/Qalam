using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class SaudiAcademicTermsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var saudiCurriculumId = 1; // Saudi Curriculum
        
        if (!await context.AcademicTerms.AnyAsync(at => at.CurriculumId == saudiCurriculumId))
        {
            var terms = new List<AcademicTerm>
            {
                new()
                {
                    CurriculumId = saudiCurriculumId,
                    NameAr = "الفصل الدراسي الأول",
                    NameEn = "First Term",
                    OrderIndex = 1,
                    IsMandatory = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    CurriculumId = saudiCurriculumId,
                    NameAr = "الفصل الدراسي الثاني",
                    NameEn = "Second Term",
                    OrderIndex = 2,
                    IsMandatory = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    CurriculumId = saudiCurriculumId,
                    NameAr = "الفصل الدراسي الثالث",
                    NameEn = "Third Term",
                    OrderIndex = 3,
                    IsMandatory = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.AcademicTerms.AddRangeAsync(terms);
            await context.SaveChangesAsync();
        }
    }
}

