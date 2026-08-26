using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Excel sharia domain: parent categories, specialties, audience levels, education type + book writables.
/// </summary>
public static class ShariaCatalogSeeder
{
    private const string ExcelCategoryMarker = "sharia.category.sharia-sciences";

    private static readonly string[] CanonicalWritableSlotCodes =
    [
        WritableFilterSlotCodes.ShariaEducationType,
        WritableFilterSlotCodes.ShariaBook,
    ];

    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.Sharia);
        if (domain is null)
            return;

        await EnsureLevelsAsync(context, domain.Id);
        await EnsureSubjectsAsync(context, domain.Id);
        await EnsureWritableSlotsAsync(context, domain.Id);
    }

    private static (string Ar, string En)[] ExcelLevels() =>
    [
        ("طلبة العلم المبتدئين", "Beginner students of knowledge"),
        ("طلبة العلم المتوسطين", "Intermediate students of knowledge"),
        ("الأطفال", "Children"),
        ("الأعاجم (غير المتحدثين بالعربية)", "Non-Arabic speakers"),
        ("المسلمون الجدد", "New Muslims")
    ];

    private static async Task EnsureLevelsAsync(ApplicationDBContext context, int domainId)
    {
        var levels = ExcelLevels();
        var existing = await context.EducationLevels
            .Where(l => l.DomainId == domainId)
            .ToListAsync();
        var byNameEn = existing.ToDictionary(l => l.NameEn, StringComparer.OrdinalIgnoreCase);
        var dirty = false;

        for (var i = 0; i < levels.Length; i++)
        {
            var (ar, en) = levels[i];
            var order = i + 1;
            if (byNameEn.TryGetValue(en, out var level))
            {
                if (!level.IsActive) { level.IsActive = true; dirty = true; }
                if (level.OrderIndex != order) { level.OrderIndex = order; dirty = true; }
                if (level.NameAr != ar) { level.NameAr = ar; dirty = true; }
                continue;
            }

            context.EducationLevels.Add(new EducationLevel
            {
                DomainId = domainId,
                NameAr = ar,
                NameEn = en,
                OrderIndex = order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            dirty = true;
        }

        if (dirty)
            await context.SaveChangesAsync();
    }

    private static async Task EnsureSubjectsAsync(ApplicationDBContext context, int domainId)
    {
        await EnsureTreeAsync(context, domainId, ShariaCategories());
        await DeactivateLegacyShariaRootsAsync(context, domainId);
    }

    private static IReadOnlyList<(string Code, string Ar, string En, (string Code, string Ar, string En)[] Children)> ShariaCategories() =>
    [
        ("sharia.category.sharia-sciences", "العلوم الشرعية", "Sharia sciences",
        [
            ("sharia.spec.aqidah", "العقيدة", "Aqidah"),
            ("sharia.spec.fiqh", "الفقه", "Fiqh"),
            ("sharia.spec.usul-fiqh", "أصول الفقه", "Usul al-Fiqh"),
            ("sharia.spec.hadith", "الحديث", "Hadith"),
            ("sharia.spec.mustalah", "مصطلح الحديث", "Hadith terminology"),
            ("sharia.spec.tafsir", "التفسير", "Tafsir"),
            ("sharia.spec.seerah", "السيرة النبوية", "Prophetic biography"),
            ("sharia.spec.akhlaq", "الأخلاق والآداب الإسلامية", "Islamic ethics and manners")
        ]),
        ("sharia.category.arabic-sciences", "علوم اللغة العربية", "Arabic language sciences",
        [
            ("sharia.spec.nahw", "النحو", "Nahw"),
            ("sharia.spec.sarf", "الصرف", "Sarf"),
            ("sharia.spec.balagha", "البلاغة", "Balagha"),
            ("sharia.spec.matn-lugha", "متن اللغة", "Lexicon matn"),
            ("sharia.spec.adab", "الأدب والشعر", "Literature and poetry"),
            ("sharia.spec.arud", "العروض", "Prosody"),
            ("sharia.spec.imla", "الإملاء", "Spelling"),
            ("sharia.spec.taabeer", "التعبير", "Composition"),
            ("sharia.spec.khat", "الخط العربي", "Arabic calligraphy")
        ])
    ];

    private static async Task DeactivateLegacyShariaRootsAsync(ApplicationDBContext context, int domainId)
    {
        var hasExcelMarker = await context.Subjects.AnyAsync(s =>
            s.DomainId == domainId &&
            s.IsActive &&
            s.Code == ExcelCategoryMarker);
        if (!hasExcelMarker)
            return;

        var legacyRoots = await context.Subjects
            .Where(s =>
                s.DomainId == domainId &&
                s.IsActive &&
                s.ParentSubjectId == null &&
                (s.Code == null || !s.Code.StartsWith("sharia.")))
            .ToListAsync();

        if (legacyRoots.Count == 0)
            return;

        foreach (var subject in legacyRoots)
        {
            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureTreeAsync(
        ApplicationDBContext context,
        int domainId,
        IReadOnlyList<(string Code, string Ar, string En, (string Code, string Ar, string En)[] Children)> categories)
    {
        var existingCodes = await context.Subjects
            .Where(s => s.DomainId == domainId && s.Code != null)
            .Select(s => s.Code!)
            .ToListAsync();
        var have = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var addedAny = false;

        foreach (var (code, ar, en, children) in categories)
        {
            Subject parent;
            if (have.Contains(code))
            {
                parent = await context.Subjects
                    .FirstAsync(s => s.DomainId == domainId && s.Code == code);
            }
            else
            {
                parent = new Subject
                {
                    DomainId = domainId,
                    Code = code,
                    NameAr = ar,
                    NameEn = en,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Subjects.Add(parent);
                await context.SaveChangesAsync();
                have.Add(code);
                addedAny = true;
            }

            foreach (var child in children)
            {
                if (have.Contains(child.Code))
                    continue;

                context.Subjects.Add(new Subject
                {
                    DomainId = domainId,
                    ParentSubjectId = parent.Id,
                    Code = child.Code,
                    NameAr = child.Ar,
                    NameEn = child.En,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                have.Add(child.Code);
                addedAny = true;
            }
        }

        if (addedAny)
            await context.SaveChangesAsync();
    }

    private static (string Code, string Ar, string En, int Order, bool Required, (string Code, string Ar, string En)[] Values)[] WritableSlotSpecs() =>
    [
        (WritableFilterSlotCodes.ShariaEducationType, "التعليم", "Education type", 1, true,
        [
            ("hifz", "حفظ وتسميع", "Memorization and audition"),
            ("sharh", "شرح وتدريس", "Explanation and teaching")
        ]),
        (WritableFilterSlotCodes.ShariaBook, "الكتب والمتون أو الأبواب", "Books, mutun, or chapters", 2, false,
        [
            ("umdat-al-talib", "متن عمدة الطالب (باب الآنية)", "Umdat al-Talib (chapter of vessels)"),
            ("zad-al-mustaqni", "زاد المستقنع", "Zad al-Mustaqni"),
            ("dalil-al-talib", "دليل الطالب", "Dalil al-Talib"),
            ("ajurrumiyya", "الأجرومية", "Al-Ajurrumiyya"),
            ("qatar-al-nada", "قطر الندى", "Qatar al-Nada"),
            ("alfiyyat-ibn-malik", "ألفية ابن مالك", "Alfiyyat Ibn Malik"),
            ("al-bayquniyya", "البيقونية", "Al-Bayquniyya")
        ])
    ];

    private static async Task EnsureWritableSlotsAsync(ApplicationDBContext context, int domainId)
    {
        var hasExcelMarker = await context.Subjects.AnyAsync(s =>
            s.DomainId == domainId &&
            s.IsActive &&
            s.Code == ExcelCategoryMarker);

        var canonicalSet = new HashSet<string>(CanonicalWritableSlotCodes, StringComparer.OrdinalIgnoreCase);
        var existingSlots = await context.WritableFilterSlots
            .Where(s => s.DomainId == domainId)
            .ToListAsync();
        var have = existingSlots
            .Select(s => s.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in WritableSlotSpecs())
        {
            if (have.Contains(spec.Code))
                continue;

            var slot = new WritableFilterSlot
            {
                DomainId = domainId,
                Code = spec.Code,
                NameAr = spec.Ar,
                NameEn = spec.En,
                AfterStep = WritableFilterAfterSteps.Subject,
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

        if (!hasExcelMarker)
            return;

        var dirty = false;
        foreach (var slot in existingSlots.Where(s => s.IsActive && !canonicalSet.Contains(s.Code)))
        {
            slot.IsActive = false;
            slot.UpdatedAt = DateTime.UtcNow;
            dirty = true;
        }

        if (dirty)
            await context.SaveChangesAsync();
    }
}
