using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds Quran subject and its content units (Surahs and Parts).
/// 
/// Quran Domain Structure:
/// - Single Subject: "القرآن الكريم" (Holy Quran)
/// - ContentType selection: تحفيظ / تلاوة / تجويد (via QuranContentType)
/// - Level selection: نورانية / مبتدئ / متوسط / متقدم (via QuranLevel)
/// - Unit selection: السور أو الأجزاء (via ContentUnit)
/// </summary>
public class QuranSubjectsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var quranDomainId = 2; // Quran domain

        if (!await SeederHelper.HasAnyDataAsync(context.Subjects, s => s.DomainId == quranDomainId))
        {
            // Single subject for Quran domain
            var quranSubject = new Subject
            {
                DomainId = quranDomainId,
                NameAr = "القرآن الكريم",
                NameEn = "Holy Quran",
                DescriptionAr = "تعليم القرآن الكريم - حفظ وتلاوة وتجويد",
                DescriptionEn = "Quran education - Memorization, Recitation, and Tajweed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Subjects.AddAsync(quranSubject);
            await context.SaveChangesAsync();

            // Seed ContentUnits for the Quran subject (Surahs and Parts)
            await SeedQuranContentUnitsAsync(context, quranSubject.Id);
        }
    }

    /// <summary>
    /// Seeds ContentUnits for Quran subject from QuranSurahs and QuranParts tables
    /// </summary>
    private static async Task SeedQuranContentUnitsAsync(ApplicationDBContext context, int subjectId)
    {
        var contentUnits = new List<ContentUnit>();

        // Get all Surahs
        var surahs = await context.QuranSurahs.OrderBy(s => s.SurahNumber).ToListAsync();
        foreach (var surah in surahs)
        {
            contentUnits.Add(new ContentUnit
            {
                SubjectId = subjectId,
                NameAr = surah.NameAr,
                NameEn = surah.NameEn,
                OrderIndex = surah.SurahNumber,
                UnitTypeCode = "QuranSurah",
                QuranSurahId = surah.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Get all Parts (Juz)
        var parts = await context.QuranParts.OrderBy(p => p.PartNumber).ToListAsync();
        foreach (var part in parts)
        {
            contentUnits.Add(new ContentUnit
            {
                SubjectId = subjectId,
                NameAr = part.NameAr,
                NameEn = part.NameEn,
                OrderIndex = 1000 + part.PartNumber, // Offset to sort after Surahs
                UnitTypeCode = "QuranPart",
                QuranPartId = part.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (contentUnits.Any())
        {
            await context.ContentUnits.AddRangeAsync(contentUnits);
            await context.SaveChangesAsync();
        }
    }
}

