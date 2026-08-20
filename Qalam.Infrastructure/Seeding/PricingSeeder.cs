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
                ExchangeRateFromBase = seed.ExchangeRateFromBase,
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

        var markets = await context.PricingMarkets
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Code == PricingMarketDefaults.DefaultMarketCode ? 0 : 1)
            .Select(m => m.Code)
            .ToListAsync();

        foreach (var marketCode in markets)
            await PricingMarketRateSeeder.SeedPlaceholderRatesForMarketAsync(context, marketCode, now);
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
