using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Excel language path: age levels, CEFR grades, one subject per language, writable skill/purpose/curriculum.
/// Deactivates legacy cartesian language×CEFR×skill subjects (rows kept for FKs).
/// </summary>
public static class LanguageExcelCatalogSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.Language);
        if (domain is null)
            return;

        await DeactivateLegacyCartesianAsync(context, domain.Id);
        await SeedAgeLevelsAndCefrAsync(context, domain.Id);
        await SeedLanguageSubjectsAsync(context, domain.Id);
        await SeedWritableSlotsAsync(context, domain.Id);
    }

    private static async Task DeactivateLegacyCartesianAsync(ApplicationDBContext context, int domainId)
    {
        var legacy = await context.Subjects
            .Where(s =>
                s.DomainId == domainId &&
                s.IsActive &&
                s.Code == null &&
                s.NameEn.Contains(" - "))
            .ToListAsync();

        if (legacy.Count == 0)
            return;

        foreach (var subject in legacy)
        {
            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAgeLevelsAndCefrAsync(ApplicationDBContext context, int domainId)
    {
        var ageBands = new (string Code, string Ar, string En)[]
        {
            ("lang.age.children", "أطفال", "Children"),
            ("lang.age.youth", "شباب", "Youth"),
            ("lang.age.adults", "كبار", "Adults")
        };

        var cefr = new (string Ar, string En, int Order)[]
        {
            ("مبتدئ A1", "Beginner A1", 1),
            ("أساسي A2", "Elementary A2", 2),
            ("متوسط B1", "Intermediate B1", 3),
            ("فوق المتوسط B2", "Upper Intermediate B2", 4),
            ("متقدم C1", "Advanced C1", 5),
            ("إتقان C2", "Proficiency C2", 6)
        };

        // Deactivate old Beginner/Intermediate/Advanced bucket levels (keep rows).
        var oldBuckets = await context.EducationLevels
            .Where(l =>
                l.DomainId == domainId &&
                l.IsActive &&
                (l.NameEn == "Beginner Level" || l.NameEn == "Intermediate Level" || l.NameEn == "Advanced Level"))
            .ToListAsync();
        foreach (var level in oldBuckets)
        {
            level.IsActive = false;
            level.UpdatedAt = DateTime.UtcNow;
        }

        if (oldBuckets.Count > 0)
            await context.SaveChangesAsync();

        for (var i = 0; i < ageBands.Length; i++)
        {
            var band = ageBands[i];
            var level = await context.EducationLevels
                .FirstOrDefaultAsync(l => l.DomainId == domainId && l.NameEn == band.En);
            if (level is null)
            {
                level = new EducationLevel
                {
                    DomainId = domainId,
                    NameAr = band.Ar,
                    NameEn = band.En,
                    OrderIndex = i + 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.EducationLevels.Add(level);
                await context.SaveChangesAsync();
            }
            else if (!level.IsActive)
            {
                level.IsActive = true;
                level.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            if (await SeederHelper.HasAnyDataAsync(context.Grades, g => g.LevelId == level.Id && g.NameEn.Contains("A1")))
                continue;

            // Clear unrelated grades if any empty set — only add CEFR if none named like A1 yet
            if (await SeederHelper.HasAnyDataAsync(context.Grades, g => g.LevelId == level.Id))
            {
                // If grades exist but are not CEFR under age band, still add missing CEFR codes by NameEn
                var existingNames = await context.Grades
                    .Where(g => g.LevelId == level.Id)
                    .Select(g => g.NameEn)
                    .ToListAsync();
                foreach (var g in cefr)
                {
                    if (existingNames.Any(n => n.Equals(g.En, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    context.Grades.Add(new Grade
                    {
                        LevelId = level.Id,
                        NameAr = g.Ar,
                        NameEn = g.En,
                        OrderIndex = g.Order,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
                continue;
            }

            foreach (var g in cefr)
            {
                context.Grades.Add(new Grade
                {
                    LevelId = level.Id,
                    NameAr = g.Ar,
                    NameEn = g.En,
                    OrderIndex = g.Order,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedLanguageSubjectsAsync(ApplicationDBContext context, int domainId)
    {
        var languages = new (string Code, string Ar, string En)[]
        {
            ("lang.en", "اللغة الإنجليزية", "English"),
            ("lang.ar-nns", "اللغة العربية لغير الناطقين بها", "Arabic for non-native speakers"),
            ("lang.fr", "اللغة الفرنسية", "French"),
            ("lang.es", "اللغة الإسبانية", "Spanish"),
            ("lang.tr", "اللغة التركية", "Turkish"),
            ("lang.zh", "اللغة الصينية", "Chinese"),
            ("lang.ja", "اللغة اليابانية", "Japanese"),
            ("lang.ko", "اللغة الكورية", "Korean")
        };

        foreach (var lang in languages)
        {
            var exists = await context.Subjects.AnyAsync(s => s.DomainId == domainId && s.Code == lang.Code);
            if (exists)
                continue;

            context.Subjects.Add(new Subject
            {
                DomainId = domainId,
                Code = lang.Code,
                NameAr = lang.Ar,
                NameEn = lang.En,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedWritableSlotsAsync(ApplicationDBContext context, int domainId)
    {
        if (await SeederHelper.HasAnyDataAsync(context.WritableFilterSlots, s => s.DomainId == domainId))
            return;

        var specs = new (string Code, string Ar, string En, string After, int Order, bool Required, (string Code, string Ar, string En)[] Values)[]
        {
            (WritableFilterSlotCodes.LanguageOtherLanguage, "لغة أخرى", "Other language",
                WritableFilterAfterSteps.Subject, 1, false,
            [
                ("de", "الألمانية", "German"),
                ("it", "الإيطالية", "Italian"),
                ("ru", "الروسية", "Russian"),
                ("ur", "الأردية", "Urdu"),
                ("pt", "البرتغالية", "Portuguese"),
                ("sw", "السواحيلية", "Swahili")
            ]),
            (WritableFilterSlotCodes.LanguageSkill, "المهارة", "Skill",
                WritableFilterAfterSteps.Subject, 2, true,
            [
                ("conversation", "المحادثة والممارسة", "Conversation and practice"),
                ("rw", "القراءة والكتابة", "Reading and writing"),
                ("grammar", "القواعد", "Grammar"),
                ("vocab", "المفردات", "Vocabulary")
            ]),
            (WritableFilterSlotCodes.LanguagePurpose, "الغرض / التخصص", "Purpose / specialization",
                WritableFilterAfterSteps.Subject, 3, true,
            [
                ("systematic", "الدراسة المنهجية", "Systematic study"),
                ("foundation", "التأسيس اللغوي", "Language foundation"),
                ("business", "لغة الأعمال والعمل", "Business and work language"),
                ("travel", "السفر والسياحة", "Travel and tourism"),
                ("exams", "التحضير للاختبارات", "Exam preparation"),
                ("interviews", "المقابلات الوظيفية", "Job interviews"),
                ("conversation", "المحادثة والممارسة", "Conversation and practice"),
                ("children", "لغة للأطفال", "Language for children"),
                ("dawah", "الدعوة والشريعة", "Dawah and sharia"),
                ("stem", "العلوم والهندسة", "Science and engineering"),
                ("health", "الصحة والطب", "Health and medicine"),
                ("translation", "الترجمة", "Translation")
            ]),
            (WritableFilterSlotCodes.LanguageCurriculum, "المنهج", "Curriculum",
                WritableFilterAfterSteps.Subject, 4, false,
            [
                ("oxford", "Oxford book", "Oxford book"),
                ("headway", "Headway", "Headway"),
                ("english-file", "English File", "English File"),
                ("qcf", "QCF", "QCF"),
                ("arabiyya-bayna-yadayk", "العربية بين يديك", "Al-Arabiyya bayna yadayk")
            ])
        };

        foreach (var spec in specs)
        {
            var slot = new WritableFilterSlot
            {
                DomainId = domainId,
                Code = spec.Code,
                NameAr = spec.Ar,
                NameEn = spec.En,
                AfterStep = spec.After,
                OrderIndex = spec.Order,
                IsRequired = spec.Required,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.WritableFilterSlots.Add(slot);
            await context.SaveChangesAsync();

            foreach (var value in spec.Values)
            {
                context.WritableFilterValues.Add(new WritableFilterValue
                {
                    SlotId = slot.Id,
                    Code = value.Code,
                    NameAr = value.Ar,
                    NameEn = value.En,
                    NormalizedText = WritableFilterTextNormalizer.Normalize(value.Ar),
                    IsSeeded = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
