using Microsoft.AspNetCore.Identity;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PricingMarketService : IPricingMarketService
{
    private readonly IPricingMarketRepository _marketRepository;
    private readonly IPricingMarketResolver _marketResolver;
    private readonly UserManager<User> _userManager;

    public PricingMarketService(
        IPricingMarketRepository marketRepository,
        IPricingMarketResolver marketResolver,
        UserManager<User> userManager)
    {
        _marketRepository = marketRepository;
        _marketResolver = marketResolver;
        _userManager = userManager;
    }

    public async Task<List<PricingMarketDto>> ListActiveMarketsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _marketRepository.ListActiveAsync(cancellationToken);
        return rows.Select(m => new PricingMarketDto
        {
            Code = m.Code,
            NameEn = m.NameEn,
            NameAr = m.NameAr,
            Currency = m.Currency
        }).ToList();
    }

    public async Task<MyPricingMarketDto> GetMyMarketAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var resolved = await _marketResolver.ResolveForUserAsync(userId, cancellationToken);
        return new MyPricingMarketDto
        {
            MarketCode = resolved.MarketCode,
            Currency = resolved.Currency,
            NameEn = resolved.NameEn,
            NameAr = resolved.NameAr,
            Source = resolved.Source.ToString().ToLowerInvariant(),
            PreferredMarketCode = user?.PreferredMarketCode
        };
    }

    public async Task<MyPricingMarketDto> SetMyMarketAsync(
        int userId,
        SetMyPricingMarketDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (dto.MarketCode != null)
        {
            var marketCode = dto.MarketCode.Trim().ToLowerInvariant();
            if (!await _marketRepository.ExistsActiveAsync(marketCode, cancellationToken))
                throw new InvalidOperationException($"Pricing market '{marketCode}' is not available.");
            user.PreferredMarketCode = marketCode;
        }
        else
        {
            user.PreferredMarketCode = null;
        }

        var result = await _userManager.UpdateAsync(user);
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return await GetMyMarketAsync(userId, cancellationToken);
    }
}
