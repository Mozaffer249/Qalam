using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class NationalitiesSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "common", "Nationalities"))
            return;

        var now = DateTime.UtcNow;
        var seeds = NationalitiesDefaults.Create(now);

        foreach (var seed in seeds)
        {
            var existing = await context.Nationalities.FirstOrDefaultAsync(n => n.Code == seed.Code);
            if (existing == null)
            {
                await context.Nationalities.AddAsync(seed);
                continue;
            }

            // Backfill missing flag emoji without overwriting admin-edited flags.
            if (string.IsNullOrWhiteSpace(existing.FlagEmoji))
            {
                existing.FlagEmoji = string.IsNullOrWhiteSpace(seed.FlagEmoji)
                    ? FlagEmojiHelper.FromIso2(seed.Code)
                    : seed.FlagEmoji;
                existing.UpdatedAt = now;
            }
        }

        await context.SaveChangesAsync();
    }
}
