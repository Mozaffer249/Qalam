using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class TeacherDomainQuestionsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "teacher", "TeacherDomainQuestions"))
            return;

        var domainIdsByCode = await context.EducationDomains
            .AsNoTracking()
            .ToDictionaryAsync(d => d.Code, d => d.Id);

        if (domainIdsByCode.Count == 0)
            return;

        var seeds = TeacherDomainQuestionsDefaults.Create(domainIdsByCode);

        foreach (var seed in seeds)
        {
            var exists = await context.TeacherDomainQuestions
                .AnyAsync(q => q.DomainId == seed.DomainId && q.Code == seed.Code);
            if (!exists)
                await context.TeacherDomainQuestions.AddAsync(seed);
        }

        await context.SaveChangesAsync();

        // Legacy skills domain is inactive — hide its survey questions on existing DBs.
        if (domainIdsByCode.TryGetValue(EducationDomainCodes.Skills, out var skillsDomainId))
        {
            var skillsQuestions = await context.TeacherDomainQuestions
                .Where(q => q.DomainId == skillsDomainId && q.IsActive)
                .ToListAsync();
            foreach (var q in skillsQuestions)
            {
                q.IsActive = false;
                q.UpdatedAt = DateTime.UtcNow;
            }

            if (skillsQuestions.Count > 0)
                await context.SaveChangesAsync();
        }
    }
}
