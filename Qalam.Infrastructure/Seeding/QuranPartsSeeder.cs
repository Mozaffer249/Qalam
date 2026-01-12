using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class QuranPartsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.HasAnyDataAsync(context.QuranParts))
        {
            var parts = new List<QuranPart>
            {
                new() { PartNumber = 1, NameAr = "الجزء الأول", NameEn = "Juz 1" },
                new() { PartNumber = 2, NameAr = "الجزء الثاني", NameEn = "Juz 2" },
                new() { PartNumber = 3, NameAr = "الجزء الثالث", NameEn = "Juz 3" },
                new() { PartNumber = 4, NameAr = "الجزء الرابع", NameEn = "Juz 4" },
                new() { PartNumber = 5, NameAr = "الجزء الخامس", NameEn = "Juz 5" },
                new() { PartNumber = 6, NameAr = "الجزء السادس", NameEn = "Juz 6" },
                new() { PartNumber = 7, NameAr = "الجزء السابع", NameEn = "Juz 7" },
                new() { PartNumber = 8, NameAr = "الجزء الثامن", NameEn = "Juz 8" },
                new() { PartNumber = 9, NameAr = "الجزء التاسع", NameEn = "Juz 9" },
                new() { PartNumber = 10, NameAr = "الجزء العاشر", NameEn = "Juz 10" },
                new() { PartNumber = 11, NameAr = "الجزء الحادي عشر", NameEn = "Juz 11" },
                new() { PartNumber = 12, NameAr = "الجزء الثاني عشر", NameEn = "Juz 12" },
                new() { PartNumber = 13, NameAr = "الجزء الثالث عشر", NameEn = "Juz 13" },
                new() { PartNumber = 14, NameAr = "الجزء الرابع عشر", NameEn = "Juz 14" },
                new() { PartNumber = 15, NameAr = "الجزء الخامس عشر", NameEn = "Juz 15" },
                new() { PartNumber = 16, NameAr = "الجزء السادس عشر", NameEn = "Juz 16" },
                new() { PartNumber = 17, NameAr = "الجزء السابع عشر", NameEn = "Juz 17" },
                new() { PartNumber = 18, NameAr = "الجزء الثامن عشر", NameEn = "Juz 18" },
                new() { PartNumber = 19, NameAr = "الجزء التاسع عشر", NameEn = "Juz 19" },
                new() { PartNumber = 20, NameAr = "الجزء العشرون", NameEn = "Juz 20" },
                new() { PartNumber = 21, NameAr = "الجزء الحادي والعشرون", NameEn = "Juz 21" },
                new() { PartNumber = 22, NameAr = "الجزء الثاني والعشرون", NameEn = "Juz 22" },
                new() { PartNumber = 23, NameAr = "الجزء الثالث والعشرون", NameEn = "Juz 23" },
                new() { PartNumber = 24, NameAr = "الجزء الرابع والعشرون", NameEn = "Juz 24" },
                new() { PartNumber = 25, NameAr = "الجزء الخامس والعشرون", NameEn = "Juz 25" },
                new() { PartNumber = 26, NameAr = "الجزء السادس والعشرون", NameEn = "Juz 26" },
                new() { PartNumber = 27, NameAr = "الجزء السابع والعشرون", NameEn = "Juz 27" },
                new() { PartNumber = 28, NameAr = "الجزء الثامن والعشرون", NameEn = "Juz 28" },
                new() { PartNumber = 29, NameAr = "الجزء التاسع والعشرون", NameEn = "Juz 29" },
                new() { PartNumber = 30, NameAr = "الجزء الثلاثون", NameEn = "Juz 30" }
            };

            await context.QuranParts.AddRangeAsync(parts);
            await context.SaveChangesAsync();
        }
    }
}
