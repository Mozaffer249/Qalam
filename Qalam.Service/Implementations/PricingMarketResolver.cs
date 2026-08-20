using Microsoft.AspNetCore.Identity;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PricingMarketResolver : IPricingMarketResolver
{
    private readonly UserManager<User> _userManager;
    private readonly IPricingMarketRepository _marketRepository;

    public PricingMarketResolver(
        UserManager<User> userManager,
        IPricingMarketRepository marketRepository)
    {
        _userManager = userManager;
        _marketRepository = marketRepository;
    }

    public async Task<ResolvedPricingMarket> ResolveForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return await GetDefaultMarketAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.PreferredMarketCode))
        {
            var preferred = await _marketRepository.GetByCodeAsync(user.PreferredMarketCode, cancellationToken);
            if (preferred is { IsActive: true })
                return Map(preferred, PricingMarketResolutionSource.Preferred);
        }

        var fromNationality = PricingMarketDefaults.ResolveMarketFromCountry(user.Nationality);
        if (!string.IsNullOrWhiteSpace(fromNationality))
        {
            var market = await _marketRepository.GetByCodeAsync(fromNationality, cancellationToken);
            if (market is { IsActive: true })
                return Map(market, PricingMarketResolutionSource.Nationality);
        }

        var fromPhone = PricingMarketDefaults.ResolveMarketFromPhone(user.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(fromPhone))
        {
            var market = await _marketRepository.GetByCodeAsync(fromPhone, cancellationToken);
            if (market is { IsActive: true })
                return Map(market, PricingMarketResolutionSource.Phone);
        }

        return await GetDefaultMarketAsync(cancellationToken);
    }

    public async Task<ResolvedPricingMarket> GetMarketAsync(
        string marketCode,
        CancellationToken cancellationToken = default)
    {
        var market = await _marketRepository.GetByCodeAsync(marketCode, cancellationToken);
        if (market is not { IsActive: true })
            throw new InvalidOperationException($"Pricing market '{marketCode}' is not available.");

        return Map(market, PricingMarketResolutionSource.Default);
    }

    public async Task<string> ResolveMarketCodeAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveForUserAsync(userId, cancellationToken);
        return resolved.MarketCode;
    }

    private async Task<ResolvedPricingMarket> GetDefaultMarketAsync(CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetDefaultAsync(cancellationToken);
        if (market == null)
        {
            market = await _marketRepository.GetByCodeAsync(
                PricingMarketDefaults.DefaultMarketCode,
                cancellationToken);
        }

        if (market == null)
            throw new InvalidOperationException("Default pricing market is not configured.");

        return Map(market, PricingMarketResolutionSource.Default);
    }

    private static ResolvedPricingMarket Map(PricingMarket market, PricingMarketResolutionSource source) =>
        new()
        {
            MarketCode = market.Code,
            Currency = market.Currency,
            NameEn = market.NameEn,
            NameAr = market.NameAr,
            Source = source
        };
}
