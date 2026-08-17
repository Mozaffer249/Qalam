using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds LanguageModule units and lessons for the legacy skills domain when still active.
/// Language (lang.*) units are owned by <see cref="ExcelDomainsUnitsLessonsSeeder"/>.
/// </summary>
public static class LanguageSkillsCatalogSeeder
{
    private sealed record CatalogModule(string NameAr, string NameEn, List<(string LessonAr, string LessonEn)> Lessons);

    public static async Task SeedAsync(ApplicationDBContext context)
    {
        // Flat Excel language subjects (lang.*) are seeded by ExcelDomainsUnitsLessonsSeeder.
        await SeedSkillsCatalogAsync(context);
    }

    private static async Task SeedSkillsCatalogAsync(ApplicationDBContext context)
    {
        var skillsDomain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == "skills" && d.IsActive);
        if (skillsDomain == null)
            return;

        var curatedOverrides = new Dictionary<string, List<CatalogModule>>
        {
            ["Communication Skills"] =
            [
                new(
                    "الوحدة 1 — الاستماع الفعال",
                    "Unit 1 — Active Listening",
                    [
                        ("الدرس 1 — أساسيات الاستماع", "Lesson 1 — Listening Basics"),
                        ("الدرس 2 — التغذية الراجعة", "Lesson 2 — Giving Feedback")
                    ])
            ],
            ["Public Speaking & Presentation"] =
            [
                new(
                    "الوحدة 1 — أساسيات العرض",
                    "Unit 1 — Presentation Basics",
                    [
                        ("الدرس 1 — هيكل العرض", "Lesson 1 — Talk Structure"),
                        ("الدرس 2 — لغة الجسد", "Lesson 2 — Body Language")
                    ])
            ]
        };

        var subjects = await context.Subjects
            .Where(s => s.DomainId == skillsDomain.Id && s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync();

        if (subjects.Count == 0)
            return;

        var catalogBySubject = new Dictionary<string, List<CatalogModule>>();

        foreach (var subject in subjects)
        {
            if (curatedOverrides.TryGetValue(subject.NameEn, out var modules))
            {
                catalogBySubject[subject.NameEn] = modules;
                continue;
            }

            catalogBySubject[subject.NameEn] =
            [
                new(
                    $"الوحدة 1 — أساسيات {subject.NameAr}",
                    $"Unit 1 — {subject.NameEn} Foundations",
                    [
                        ("الدرس 1 — مقدمة", "Lesson 1 — Introduction"),
                        ("الدرس 2 — تطبيق عملي", "Lesson 2 — Practice")
                    ])
            ];
        }

        await SeedCatalogForSubjectsAsync(context, skillsDomain.Id, catalogBySubject);
    }

    private static async Task SeedCatalogForSubjectsAsync(
        ApplicationDBContext context,
        int domainId,
        Dictionary<string, List<CatalogModule>> catalogBySubject)
    {
        var contentUnits = new List<ContentUnit>();

        foreach (var (subjectNameEn, modules) in catalogBySubject)
        {
            var subject = await context.Subjects
                .FirstOrDefaultAsync(s => s.DomainId == domainId && s.NameEn == subjectNameEn);

            if (subject == null)
            {
                throw new InvalidOperationException(
                    $"Subject '{subjectNameEn}' must be seeded before catalog content for domain {domainId}");
            }

            var alreadySeeded = await context.ContentUnits.AnyAsync(cu =>
                cu.SubjectId == subject.Id && cu.UnitTypeCode == "LanguageModule");
            if (alreadySeeded)
            {
                continue;
            }

            for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
            {
                var module = modules[moduleIndex];
                var unit = new ContentUnit
                {
                    SubjectId = subject.Id,
                    TermId = null,
                    NameAr = module.NameAr,
                    NameEn = module.NameEn,
                    OrderIndex = moduleIndex + 1,
                    UnitTypeCode = "LanguageModule",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                for (var lessonIndex = 0; lessonIndex < module.Lessons.Count; lessonIndex++)
                {
                    var lesson = module.Lessons[lessonIndex];
                    unit.Lessons.Add(new Lesson
                    {
                        NameAr = lesson.LessonAr,
                        NameEn = lesson.LessonEn,
                        OrderIndex = lessonIndex + 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

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
