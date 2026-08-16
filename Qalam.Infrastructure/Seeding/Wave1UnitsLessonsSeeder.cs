using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds sample LanguageModule units and lessons for wave-1 leaf subjects (no Term).
/// </summary>
public static class Wave1UnitsLessonsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var domains = await context.EducationDomains
            .Where(d => EducationDomainCodes.Wave1SplitFromSkills.Contains(d.Code))
            .ToListAsync();
        if (domains.Count == 0)
            return;

        var contentUnits = new List<ContentUnit>();

        foreach (var domain in domains)
        {
            // Leaf = active subject that is not a parent of another subject.
            var leafSubjects = await context.Subjects
                .Where(s =>
                    s.DomainId == domain.Id &&
                    s.IsActive &&
                    !context.Subjects.Any(c => c.ParentSubjectId == s.Id))
                .OrderBy(s => s.Id)
                .ToListAsync();

            foreach (var subject in leafSubjects)
            {
                var alreadySeeded = await context.ContentUnits.AnyAsync(cu =>
                    cu.SubjectId == subject.Id && cu.UnitTypeCode == "LanguageModule");
                if (alreadySeeded)
                    continue;

                var unit = new ContentUnit
                {
                    SubjectId = subject.Id,
                    TermId = null,
                    NameAr = $"مقدمة — {subject.NameAr}",
                    NameEn = $"Foundations — {subject.NameEn}",
                    OrderIndex = 1,
                    UnitTypeCode = "LanguageModule",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                unit.Lessons.Add(new Lesson
                {
                    NameAr = "الدرس 1 — مقدمة",
                    NameEn = "Lesson 1 — Introduction",
                    OrderIndex = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
                unit.Lessons.Add(new Lesson
                {
                    NameAr = "الدرس 2 — تطبيق عملي",
                    NameEn = "Lesson 2 — Practice",
                    OrderIndex = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });

                contentUnits.Add(unit);
            }
        }

        if (contentUnits.Count > 0)
        {
            await context.ContentUnits.AddRangeAsync(contentUnits);
            await context.SaveChangesAsync();
        }
    }
}
