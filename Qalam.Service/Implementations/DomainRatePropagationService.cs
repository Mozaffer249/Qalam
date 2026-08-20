using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Service.Implementations;

public interface IDomainRatePropagationService
{
    Task<DomainSessionPrice> PropagateBaseRateAsync(
        int domainId,
        string sessionTypeCode,
        decimal basePricePerHour,
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default);

    Task RecalculateMarketFromBaseAsync(
        string marketCode,
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default);
}

public class DomainRatePropagationService : IDomainRatePropagationService
{
    private readonly IDomainSessionPriceRepository _priceRepository;
    private readonly IPricingMarketRepository _marketRepository;

    public DomainRatePropagationService(
        IDomainSessionPriceRepository priceRepository,
        IPricingMarketRepository marketRepository)
    {
        _priceRepository = priceRepository;
        _marketRepository = marketRepository;
    }

    public static decimal DeriveLocalPrice(decimal basePrice, decimal exchangeRateFromBase) =>
        PricingExchangeRateHelper.DeriveLocalPrice(basePrice, exchangeRateFromBase);

    public async Task<DomainSessionPrice> PropagateBaseRateAsync(
        int domainId,
        string sessionTypeCode,
        decimal basePricePerHour,
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default)
    {
        var baseCode = PricingMarketDefaults.DefaultMarketCode;
        var baseRow = await UpsertRateAsync(
            domainId,
            sessionTypeCode,
            baseCode,
            basePricePerHour,
            effectiveFrom,
            cancellationToken);

        var markets = await _marketRepository.ListActiveAsync(cancellationToken);
        foreach (var market in markets.Where(m => m.Code != baseCode))
        {
            var derived = DeriveLocalPrice(basePricePerHour, market.ExchangeRateFromBase);
            await UpsertRateAsync(
                domainId,
                sessionTypeCode,
                market.Code,
                derived,
                effectiveFrom,
                cancellationToken);
        }

        await _priceRepository.SaveChangesAsync();
        return baseRow;
    }

    public async Task RecalculateMarketFromBaseAsync(
        string marketCode,
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default)
    {
        var normalized = marketCode.Trim().ToLowerInvariant();
        if (normalized == PricingMarketDefaults.DefaultMarketCode)
            throw new InvalidOperationException("Cannot recalculate base market rates from exchange rate.");

        var market = await _marketRepository.GetByCodeAsync(normalized, cancellationToken);
        if (market == null || !market.IsActive)
            throw new InvalidOperationException($"Pricing market '{normalized}' is not available.");

        var baseRates = await _priceRepository.ListCurrentRatesAsync(
            PricingMarketDefaults.DefaultMarketCode,
            cancellationToken);

        foreach (var baseRate in baseRates)
        {
            var derived = DeriveLocalPrice(baseRate.PricePerHour, market.ExchangeRateFromBase);
            await UpsertRateAsync(
                baseRate.DomainId,
                baseRate.SessionTypeCode,
                normalized,
                derived,
                effectiveFrom,
                cancellationToken);
        }

        await _priceRepository.SaveChangesAsync();
    }

    private async Task<DomainSessionPrice> UpsertRateAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        decimal pricePerHour,
        DateTime effectiveFrom,
        CancellationToken cancellationToken)
    {
        var current = await _priceRepository.GetCurrentRateAsync(
            domainId, sessionTypeCode, marketCode, cancellationToken);
        if (current != null && current.PricePerHour == pricePerHour)
            return current;

        await _priceRepository.CloseCurrentRateAsync(
            domainId, sessionTypeCode, marketCode, effectiveFrom, cancellationToken);

        var row = new DomainSessionPrice
        {
            MarketCode = marketCode,
            DomainId = domainId,
            SessionTypeCode = sessionTypeCode,
            PricePerHour = pricePerHour,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _priceRepository.AddAsync(row);
        return row;
    }
}
