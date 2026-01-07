using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class SessionTypesSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await context.SessionTypes.AnyAsync())
        {
            var sessionTypes = new List<SessionType>
            {
                new()
                {
                    Code = "individual",
                    NameAr = "فردية",
                    NameEn = "Individual",
                    DescriptionAr = "جلسة خاصة لطالب واحد",
                    DescriptionEn = "Private session for one student"
                },
                new()
                {
                    Code = "group",
                    NameAr = "جماعية",
                    NameEn = "Group",
                    DescriptionAr = "جلسة لمجموعة من الطلاب",
                    DescriptionEn = "Session for a group of students"
                }
            };

            await context.SessionTypes.AddRangeAsync(sessionTypes);
            await context.SaveChangesAsync();
        }
    }
}

