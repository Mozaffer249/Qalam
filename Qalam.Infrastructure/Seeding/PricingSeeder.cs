using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding;

namespace Qalam.Infrastructure.Seeding;

public static class PricingSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "teacher", "TeacherLevels"))
            return;

        var now = DateTime.UtcNow;
        await SeedTeacherLevelsAsync(context, now);
        await SeedDomainSessionPricesAsync(context, now);
        await AssignStarterLevelToTeachersWithoutLevelAsync(context);
    }

    private static async Task SeedTeacherLevelsAsync(ApplicationDBContext context, DateTime now)
    {
        foreach (var seed in PricingDefaults.CreateTeacherLevels(now))
        {
            var existing = await context.TeacherLevels.FirstOrDefaultAsync(l => l.Code == seed.Code);
            if (existing == null)
                await context.TeacherLevels.AddAsync(seed);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDomainSessionPricesAsync(ApplicationDBContext context, DateTime now)
    {
        if (!await SeederHelper.TableExistsAsync(context, "pricing", "DomainSessionPrices"))
            return;

        var domains = await context.EducationDomains
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new { d.Id, d.Code })
            .ToListAsync();

        foreach (var domain in domains)
        {
            var (individual, group) = PricingDefaults.GetDomainRates(domain.Code);
            await EnsureRateAsync(context, domain.Id, PricingDefaults.SessionTypeIndividual, individual, now);
            await EnsureRateAsync(context, domain.Id, PricingDefaults.SessionTypeGroup, group, now);
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRateAsync(
        ApplicationDBContext context,
        int domainId,
        string sessionTypeCode,
        decimal pricePerHour,
        DateTime now)
    {
        var hasCurrent = await context.DomainSessionPrices.AnyAsync(p =>
            p.DomainId == domainId
            && p.SessionTypeCode == sessionTypeCode
            && p.EffectiveTo == null);

        if (hasCurrent)
            return;

        await context.DomainSessionPrices.AddAsync(new DomainSessionPrice
        {
            DomainId = domainId,
            SessionTypeCode = sessionTypeCode,
            PricePerHour = pricePerHour,
            EffectiveFrom = now,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = now
        });
    }

    private static async Task AssignStarterLevelToTeachersWithoutLevelAsync(ApplicationDBContext context)
    {
        var starter = await context.TeacherLevels.AsNoTracking()
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync();
        if (starter == null)
            return;

        var teachers = await context.Teachers
            .Where(t => t.TeacherLevelId == null)
            .ToListAsync();

        foreach (var teacher in teachers)
            teacher.TeacherLevelId = starter.Id;

        if (teachers.Count > 0)
            await context.SaveChangesAsync();
    }
}
