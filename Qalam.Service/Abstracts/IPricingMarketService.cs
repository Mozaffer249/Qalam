using Qalam.Data.DTOs.Pricing;

namespace Qalam.Service.Abstracts;

public interface IPricingMarketService
{
    Task<List<PricingMarketDto>> ListActiveMarketsAsync(CancellationToken cancellationToken = default);

    Task<MyPricingMarketDto> GetMyMarketAsync(int userId, CancellationToken cancellationToken = default);

    Task<MyPricingMarketDto> SetMyMarketAsync(
        int userId,
        SetMyPricingMarketDto dto,
        CancellationToken cancellationToken = default);
}
