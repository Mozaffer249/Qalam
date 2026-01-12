using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class DatabaseSeeder
{
    public static async Task SeedAllAsync(ApplicationDBContext context)
    {
        // Seed in correct order to respect dependencies

        // Basic infrastructure
        await EducationDomainsSeeder.SeedAsync(context);
        await CurriculumsSeeder.SeedAsync(context);
        await TeachingModesSeeder.SeedAsync(context);
        await SessionTypesSeeder.SeedAsync(context);
        await QuranLevelsSeeder.SeedAsync(context);
        await QuranContentTypesSeeder.SeedAsync(context);
        await QuranPartsSeeder.SeedAsync(context);
        await QuranSurahsSeeder.SeedAsync(context);
        await TimeSlotsSeeder.SeedAsync(context);

        // Saudi Education System (School Domain)
        await SaudiEducationLevelsSeeder.SeedAsync(context);
        await SaudiGradesSeeder.SeedAsync(context);
        await SaudiAcademicTermsSeeder.SeedAsync(context);
        await SaudiSubjectsSeeder.SeedAsync(context);

        // Quran Domain Subjects
        await QuranSubjectsSeeder.SeedAsync(context);

        // Languages Domain
        await LanguageLevelsSeeder.SeedAsync(context);
        await LanguageSubjectsSeeder.SeedAsync(context);

        // General Skills Domain
        await GeneralSkillsSubjectsSeeder.SeedAsync(context);
    }
}

