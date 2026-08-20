using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds domain session rates for a pricing market from SAR base rates × exchange rate.
/// </summary>
public static class PricingMarketRateSeeder
{
    public static async Task SeedPlaceholderRatesForMarketAsync(
        ApplicationDBContext context,
        string marketCode,
        DateTime? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var now = asOf ?? DateTime.UtcNow;
        var normalized = marketCode.Trim().ToLowerInvariant();
        var market = await context.PricingMarkets
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == normalized, cancellationToken);

        if (market == null)
            return;

        var domains = await context.EducationDomains
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new { d.Id, d.Code })
            .ToListAsync(cancellationToken);

        if (normalized == PricingMarketDefaults.DefaultMarketCode)
        {
            foreach (var domain in domains)
            {
                var (individual, group) = PricingDefaults.GetDomainRates(domain.Code);
                await EnsureRateAsync(context, normalized, domain.Id, PricingDefaults.SessionTypeIndividual, individual, now, cancellationToken);
                await EnsureRateAsync(context, normalized, domain.Id, PricingDefaults.SessionTypeGroup, group, now, cancellationToken);
            }
        }
        else
        {
            var baseRates = await context.DomainSessionPrices
                .AsNoTracking()
                .Where(p =>
                    p.MarketCode == PricingMarketDefaults.DefaultMarketCode
                    && p.IsActive
                    && p.EffectiveTo == null)
                .Select(p => new { p.DomainId, p.SessionTypeCode, p.PricePerHour })
                .ToListAsync(cancellationToken);

            if (baseRates.Count > 0)
            {
                foreach (var baseRate in baseRates)
                {
                    var derived = PricingExchangeRateHelper.DeriveLocalPrice(
                        baseRate.PricePerHour,
                        market.ExchangeRateFromBase);
                    await EnsureRateAsync(
                        context,
                        normalized,
                        baseRate.DomainId,
                        baseRate.SessionTypeCode,
                        derived,
                        now,
                        cancellationToken);
                }
            }
            else
            {
                foreach (var domain in domains)
                {
                    var (individual, group) = PricingDefaults.GetDomainRates(domain.Code);
                    await EnsureRateAsync(
                        context,
                        normalized,
                        domain.Id,
                        PricingDefaults.SessionTypeIndividual,
                        PricingExchangeRateHelper.DeriveLocalPrice(individual, market.ExchangeRateFromBase),
                        now,
                        cancellationToken);
                    await EnsureRateAsync(
                        context,
                        normalized,
                        domain.Id,
                        PricingDefaults.SessionTypeGroup,
                        PricingExchangeRateHelper.DeriveLocalPrice(group, market.ExchangeRateFromBase),
                        now,
                        cancellationToken);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRateAsync(
        ApplicationDBContext context,
        string marketCode,
        int domainId,
        string sessionTypeCode,
        decimal pricePerHour,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var hasCurrent = await context.DomainSessionPrices.AnyAsync(p =>
            p.MarketCode == marketCode
            && p.DomainId == domainId
            && p.SessionTypeCode == sessionTypeCode
            && p.EffectiveTo == null,
            cancellationToken);

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
        }, cancellationToken);
    }
}
