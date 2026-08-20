namespace Qalam.Service.Abstracts;

public enum PricingMarketResolutionSource
{
    Preferred,
    Nationality,
    Phone,
    Default
}

public sealed class ResolvedPricingMarket
{
    public string MarketCode { get; init; } = default!;
    public string Currency { get; init; } = default!;
    public string NameEn { get; init; } = default!;
    public string NameAr { get; init; } = default!;
    public PricingMarketResolutionSource Source { get; init; }
}

public interface IPricingMarketResolver
{
    Task<ResolvedPricingMarket> ResolveForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<ResolvedPricingMarket> GetMarketAsync(string marketCode, CancellationToken cancellationToken = default);

    Task<string> ResolveMarketCodeAsync(int userId, CancellationToken cancellationToken = default);
}
