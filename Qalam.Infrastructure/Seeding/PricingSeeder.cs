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
        await SeedPricingMarketsAsync(context, now);
        await BackfillExistingRatesToSaMarketAsync(context);
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

    private static async Task SeedPricingMarketsAsync(ApplicationDBContext context, DateTime now)
    {
        if (!await SeederHelper.TableExistsAsync(context, "pricing", "PricingMarkets"))
            return;

        foreach (var seed in PricingMarketDefaults.CreateMarkets(now))
        {
            var existing = await context.PricingMarkets.FirstOrDefaultAsync(m => m.Code == seed.Code);
            if (existing != null)
                continue;

            await context.PricingMarkets.AddAsync(new PricingMarket
            {
                Code = seed.Code,
                Currency = seed.Currency,
                NameEn = seed.NameEn,
                NameAr = seed.NameAr,
                IsActive = true,
                IsDefault = seed.IsDefault,
                CreatedAt = seed.CreatedAt
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task BackfillExistingRatesToSaMarketAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "pricing", "DomainSessionPrices"))
            return;

        await context.DomainSessionPrices
            .Where(p => string.IsNullOrEmpty(p.MarketCode))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MarketCode, PricingMarketDefaults.DefaultMarketCode));
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

        var markets = await context.PricingMarkets
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => m.Code)
            .ToListAsync();

        foreach (var marketCode in markets)
        {
            foreach (var domain in domains)
            {
                var (individual, group) = PricingDefaults.GetDomainRates(domain.Code);
                var placeholderMultiplier = marketCode == PricingMarketDefaults.DefaultMarketCode
                    ? 1m
                    : GetPlaceholderMultiplier(marketCode);

                await EnsureRateAsync(
                    context,
                    marketCode,
                    domain.Id,
                    PricingDefaults.SessionTypeIndividual,
                    Math.Round(individual * placeholderMultiplier, 2, MidpointRounding.AwayFromZero),
                    now);
                await EnsureRateAsync(
                    context,
                    marketCode,
                    domain.Id,
                    PricingDefaults.SessionTypeGroup,
                    Math.Round(group * placeholderMultiplier, 2, MidpointRounding.AwayFromZero),
                    now);
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Non-SA placeholder rates — not FX conversions; admin should set native prices before go-live.
    /// </summary>
    private static decimal GetPlaceholderMultiplier(string marketCode) => marketCode switch
    {
        "ae" => 1.0m,
        "kw" => 0.08m,
        "qa" => 1.0m,
        "bh" => 0.10m,
        "om" => 0.10m,
        "eg" => 8.0m,
        "jo" => 0.19m,
        _ => 1.0m
    };

    private static async Task EnsureRateAsync(
        ApplicationDBContext context,
        string marketCode,
        int domainId,
        string sessionTypeCode,
        decimal pricePerHour,
        DateTime now)
    {
        var hasCurrent = await context.DomainSessionPrices.AnyAsync(p =>
            p.MarketCode == marketCode
            && p.DomainId == domainId
            && p.SessionTypeCode == sessionTypeCode
            && p.EffectiveTo == null);

        if (hasCurrent)
            return;

        await context.DomainSessionPrices.AddAsync(new DomainSessionPrice
        {
            MarketCode = marketCode,
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
            .Where(l => l.IsActive)
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync();
        if (starter == null)
            return;

        await context.Teachers
            .Where(t => t.TeacherLevelId == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.TeacherLevelId, starter.Id)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));
    }
}
