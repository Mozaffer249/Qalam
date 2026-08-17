using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds sample units and lessons for Excel domains that expose content trees:
/// sharia → LanguageModule; university → SchoolUnit.
/// Language has no units/lessons per Excel (stops at writables).
/// </summary>
public static class ExcelDomainsUnitsLessonsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        await DeactivateLanguageDummyUnitsAsync(context);

        await SeedDomainAsync(
            context,
            EducationDomainCodes.Sharia,
            unitTypeCode: "LanguageModule",
            subjectFilter: null);

        await SeedDomainAsync(
            context,
            EducationDomainCodes.University,
            unitTypeCode: "SchoolUnit",
            subjectFilter: null);
    }

    /// <summary>
    /// Excel language path has no units — deactivate any LanguageModule rows under lang.* subjects.
    /// </summary>
    private static async Task DeactivateLanguageDummyUnitsAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.Language);
        if (domain is null)
            return;

        var subjectIds = await context.Subjects
            .Where(s => s.DomainId == domain.Id && s.Code != null && s.Code.StartsWith("lang."))
            .Select(s => s.Id)
            .ToListAsync();
        if (subjectIds.Count == 0)
            return;

        var units = await context.ContentUnits
            .Include(cu => cu.Lessons)
            .Where(cu =>
                subjectIds.Contains(cu.SubjectId) &&
                cu.UnitTypeCode == "LanguageModule" &&
                cu.IsActive)
            .ToListAsync();

        if (units.Count == 0)
            return;

        foreach (var unit in units)
        {
            unit.IsActive = false;
            unit.UpdatedAt = DateTime.UtcNow;
            foreach (var lesson in unit.Lessons.Where(l => l.IsActive))
            {
                lesson.IsActive = false;
                lesson.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDomainAsync(
        ApplicationDBContext context,
        string domainCode,
        string unitTypeCode,
        Func<Subject, bool>? subjectFilter)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == domainCode);
        if (domain is null)
            return;

        var leafQuery = context.Subjects
            .Where(s =>
                s.DomainId == domain.Id &&
                s.IsActive &&
                !context.Subjects.Any(c => c.ParentSubjectId == s.Id));

        var leafSubjects = await leafQuery
            .OrderBy(s => s.Id)
            .ToListAsync();

        if (subjectFilter is not null)
            leafSubjects = leafSubjects.Where(subjectFilter).ToList();

        var contentUnits = new List<ContentUnit>();

        foreach (var subject in leafSubjects)
        {
            var alreadySeeded = await context.ContentUnits.AnyAsync(cu =>
                cu.SubjectId == subject.Id && cu.UnitTypeCode == unitTypeCode);
            if (alreadySeeded)
                continue;

            var unit = new ContentUnit
            {
                SubjectId = subject.Id,
                TermId = null,
                NameAr = $"مقدمة — {subject.NameAr}",
                NameEn = $"Foundations — {subject.NameEn}",
                OrderIndex = 1,
                UnitTypeCode = unitTypeCode,
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

        if (contentUnits.Count > 0)
        {
            await context.ContentUnits.AddRangeAsync(contentUnits);
            await context.SaveChangesAsync();
        }
    }
}
