using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class DatabaseSeeder
{
    public static async Task SeedAllAsync(ApplicationDBContext context)
    {
        // Seed in correct order to respect dependencies
        await EducationDomainsSeeder.SeedAsync(context);
        await CurriculumsSeeder.SeedAsync(context);
        await TeachingModesSeeder.SeedAsync(context);
        await SessionTypesSeeder.SeedAsync(context);
        await QuranLevelsSeeder.SeedAsync(context);
        await QuranContentTypesSeeder.SeedAsync(context);
        await TimeSlotsSeeder.SeedAsync(context);
    }
}

