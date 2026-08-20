using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds placeholder domain session rates for a pricing market.
/// Non-SA rates are placeholders — admin should set native prices before go-live.
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
        var domains = await context.EducationDomains
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new { d.Id, d.Code })
            .ToListAsync(cancellationToken);

        foreach (var domain in domains)
        {
            var (individual, group) = PricingDefaults.GetDomainRates(domain.Code);
            var multiplier = marketCode == PricingMarketDefaults.DefaultMarketCode
                ? 1m
                : GetPlaceholderMultiplier(marketCode);

            await EnsureRateAsync(
                context,
                marketCode,
                domain.Id,
                PricingDefaults.SessionTypeIndividual,
                Math.Round(individual * multiplier, 2, MidpointRounding.AwayFromZero),
                now,
                cancellationToken);
            await EnsureRateAsync(
                context,
                marketCode,
                domain.Id,
                PricingDefaults.SessionTypeGroup,
                Math.Round(group * multiplier, 2, MidpointRounding.AwayFromZero),
                now,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    internal static decimal GetPlaceholderMultiplier(string marketCode) => marketCode switch
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
