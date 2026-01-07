using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class TeachingModesSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await context.TeachingModes.AnyAsync())
        {
            var teachingModes = new List<TeachingMode>
            {
                new()
                {
                    Code = "in_person",
                    NameAr = "حضوري",
                    NameEn = "In-Person",
                    DescriptionAr = "التدريس وجهاً لوجه في موقع محدد",
                    DescriptionEn = "Face-to-face teaching at a specific location"
                },
                new()
                {
                    Code = "online",
                    NameAr = "عن بُعد",
                    NameEn = "Online",
                    DescriptionAr = "التدريس عبر الإنترنت",
                    DescriptionEn = "Teaching via the internet"
                }
            };

            await context.TeachingModes.AddRangeAsync(teachingModes);
            await context.SaveChangesAsync();
        }
    }
}

