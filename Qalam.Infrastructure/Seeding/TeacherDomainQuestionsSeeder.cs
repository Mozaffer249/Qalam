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

        var domains = await context.EducationDomains
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new { d.Id, d.Code })
            .ToListAsync();

        var domainIdsByCode = domains
            .GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        if (domainIdsByCode.Count == 0)
            return;

        // Domains that already have admin/custom questions — do not pile on short system defaults.
        var domainsWithCustomQuestions = await context.TeacherDomainQuestions
            .AsNoTracking()
            .Where(q => q.IsActive && !q.IsSystem)
            .Select(q => q.DomainId)
            .Distinct()
            .ToListAsync();
        var skipDefaults = domainsWithCustomQuestions.ToHashSet();

        var seeds = TeacherDomainQuestionsDefaults.Create(domainIdsByCode);

        foreach (var seed in seeds)
        {
            if (skipDefaults.Contains(seed.DomainId))
                continue;

            var exists = await context.TeacherDomainQuestions
                .AnyAsync(q => q.DomainId == seed.DomainId && q.Code == seed.Code);
            if (!exists)
                await context.TeacherDomainQuestions.AddAsync(seed);
        }

        await context.SaveChangesAsync();

        // Only hide skills survey questions when the skills domain itself is inactive.
        if (domainIdsByCode.TryGetValue(EducationDomainCodes.Skills, out var skillsDomainId))
        {
            var skillsActive = await context.EducationDomains
                .AsNoTracking()
                .AnyAsync(d => d.Id == skillsDomainId && d.IsActive);
            if (skillsActive)
                return;

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
