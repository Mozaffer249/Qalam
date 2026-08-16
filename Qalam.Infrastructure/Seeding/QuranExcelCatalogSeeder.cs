using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Excel Quran filters: audience EducationLevels + riwayah writable slot.
/// </summary>
public static class QuranExcelCatalogSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var domain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.Quran);
        if (domain is null)
            return;

        await SeedAudienceLevelsAsync(context, domain.Id);
        await SeedRiwayahSlotAsync(context, domain.Id);
    }

    private static async Task SeedAudienceLevelsAsync(ApplicationDBContext context, int domainId)
    {
        if (await SeederHelper.HasAnyDataAsync(
                context.EducationLevels,
                l => l.DomainId == domainId && l.CurriculumId == null && l.AcademicProgramId == null))
            return;

        var levels = new (string Ar, string En)[]
        {
            ("الصغار", "Children"),
            ("الكبار", "Adults"),
            ("الأعاجم", "Non-Arabic speakers"),
            ("طلبة القراءات والمتخصصون", "Qira'at students and specialists")
        };

        for (var i = 0; i < levels.Length; i++)
        {
            context.EducationLevels.Add(new EducationLevel
            {
                DomainId = domainId,
                NameAr = levels[i].Ar,
                NameEn = levels[i].En,
                OrderIndex = i + 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRiwayahSlotAsync(ApplicationDBContext context, int domainId)
    {
        if (await SeederHelper.HasAnyDataAsync(
                context.WritableFilterSlots,
                s => s.DomainId == domainId && s.Code == WritableFilterSlotCodes.QuranRiwayah))
            return;

        var slot = new WritableFilterSlot
        {
            DomainId = domainId,
            Code = WritableFilterSlotCodes.QuranRiwayah,
            NameAr = "القراءة",
            NameEn = "Riwayah",
            AfterStep = WritableFilterAfterSteps.Subject,
            OrderIndex = 1,
            IsRequired = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.WritableFilterSlots.Add(slot);
        await context.SaveChangesAsync();

        var riwayat = new (string Code, string Ar, string En)[]
        {
            ("hafs-asim", "حفص عن عاصم", "Hafs from Asim"),
            ("shuba-asim", "شعبة عن عاصم", "Shu'ba from Asim"),
            ("qalun-nafi", "قالون عن نافع", "Qalun from Nafi"),
            ("warsh-nafi", "ورش عن نافع", "Warsh from Nafi"),
            ("al-bazzi-ibn-kathir", "البزي عن ابن كثير", "Al-Bazzi from Ibn Kathir"),
            ("qunbul-ibn-kathir", "قنبل عن ابن كثير", "Qunbul from Ibn Kathir"),
            ("al-duri-abu-amr", "الدوري عن أبي عمرو", "Al-Duri from Abu Amr"),
            ("al-susi-abu-amr", "السوسي عن أبي عمرو", "Al-Susi from Abu Amr"),
            ("hisham-ibn-amir", "هشام عن ابن عامر", "Hisham from Ibn Amir"),
            ("ibn-dhakwan-ibn-amir", "ابن ذكوان عن ابن عامر", "Ibn Dhakwan from Ibn Amir"),
            ("khalaf-hamza", "خلف عن حمزة", "Khalaf from Hamza"),
            ("khallad-hamza", "خلاد عن حمزة", "Khallad from Hamza"),
            ("abu-al-harith-al-kisai", "أبو الحارث عن الكسائي", "Abu al-Harith from al-Kisai"),
            ("al-duri-al-kisai", "الدوري عن الكسائي", "Al-Duri from al-Kisai"),
            ("ibn-wardan-abu-jafar", "ابن وردان عن أبي جعفر", "Ibn Wardan from Abu Ja'far"),
            ("ibn-jammaz-abu-jafar", "ابن جماز عن أبي جعفر", "Ibn Jammaz from Abu Ja'far"),
            ("ruways-yaqub", "رويس عن يعقوب", "Ruways from Ya'qub"),
            ("rawh-yaqub", "روح عن يعقوب", "Rawh from Ya'qub"),
            ("ishaq-khalaf-10", "إسحاق عن خلف العاشر", "Ishaq from Khalaf al-Ashir"),
            ("idris-khalaf-10", "إدريس عن خلف العاشر", "Idris from Khalaf al-Ashir")
        };

        foreach (var r in riwayat)
        {
            context.WritableFilterValues.Add(new WritableFilterValue
            {
                SlotId = slot.Id,
                Code = r.Code,
                NameAr = r.Ar,
                NameEn = r.En,
                NormalizedText = WritableFilterTextNormalizer.Normalize(r.Ar),
                IsSeeded = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}
