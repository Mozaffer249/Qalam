using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class QuranContentTypesSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var desired = new (string Code, string Ar, string En)[]
        {
            ("memorization", "الحفظ والمراجعة والتسميع", "Memorization, review, and audition"),
            ("recitation", "تصحيح وتعليم التلاوة", "Recitation correction and teaching"),
            ("norania", "النورانية", "Al-Norania"),
            ("tajweed", "التجويد", "Tajweed"),
            ("ijaza", "الإجازة والإقراء", "Ijaza and iqra"),
            ("tafsir", "التفسير والتدبر", "Tafsir and reflection"),
            ("ulum", "علوم القرآن", "Quranic sciences")
        };

        var existing = await context.QuranContentTypes.ToListAsync();
        var byCode = existing.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
        var dirty = false;

        foreach (var item in desired)
        {
            if (byCode.TryGetValue(item.Code, out var row))
            {
                if (row.NameAr != item.Ar || row.NameEn != item.En)
                {
                    row.NameAr = item.Ar;
                    row.NameEn = item.En;
                    row.UpdatedAt = DateTime.UtcNow;
                    dirty = true;
                }
                continue;
            }

            context.QuranContentTypes.Add(new QuranContentType
            {
                Code = item.Code,
                NameAr = item.Ar,
                NameEn = item.En,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            dirty = true;
        }

        if (dirty)
            await context.SaveChangesAsync();
    }
}
