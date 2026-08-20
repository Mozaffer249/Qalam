using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Implementations;

public class PricingEngine : IPricingEngine
{
    private readonly IDomainSessionPriceRepository _priceRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IPricingMarketRepository _marketRepository;

    public PricingEngine(
        IDomainSessionPriceRepository priceRepository,
        ITeacherRepository teacherRepository,
        IPricingMarketRepository marketRepository)
    {
        _priceRepository = priceRepository;
        _teacherRepository = teacherRepository;
        _marketRepository = marketRepository;
    }

    public async Task<PriceEstimate> EstimateAsync(
        PricingEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TotalMinutes <= 0)
            throw new InvalidOperationException("Total duration must be greater than zero.");

        var market = await _marketRepository.GetByCodeAsync(request.MarketCode, cancellationToken);
        if (market is not { IsActive: true })
            throw new InvalidOperationException($"Pricing market '{request.MarketCode}' is not available.");

        var asOf = request.AsOf ?? DateTime.UtcNow;
        var rate = await _priceRepository.GetEffectiveRateAsync(
            request.DomainId,
            request.SessionTypeCode,
            request.MarketCode,
            asOf,
            cancellationToken);

        if (rate == null)
            throw new InvalidOperationException(
                $"No active pricing rule for market '{request.MarketCode}', domain {request.DomainId} and session type '{request.SessionTypeCode}'.");

        var share = await ResolveTeacherShareAsync(request.TeacherId, cancellationToken);
        return BuildEstimate(rate, request.TotalMinutes, share.SharePct, share.LevelId, market.Currency);
    }

    public async Task<PricingSnapshot> CreateSnapshotAsync(
        CreatePricingSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var estimate = await EstimateAsync(new PricingEstimateRequest
        {
            DomainId = request.DomainId,
            SessionTypeCode = request.SessionTypeCode,
            MarketCode = request.MarketCode,
            TotalMinutes = request.TotalMinutes,
            TeacherId = request.TeacherId,
            AsOf = request.AsOf
        }, cancellationToken);

        return new PricingSnapshot
        {
            Context = request.Context,
            ContextEntityId = request.ContextEntityId,
            DomainId = request.DomainId,
            SessionTypeCode = request.SessionTypeCode,
            MarketCode = estimate.MarketCode,
            Currency = estimate.Currency,
            DomainSessionPriceId = estimate.DomainSessionPriceId,
            PricePerHour = estimate.PricePerHour,
            TotalMinutes = estimate.TotalMinutes,
            TotalPrice = estimate.TotalPrice,
            TeacherId = request.TeacherId,
            TeacherLevelId = estimate.TeacherLevelId,
            TeacherSharePct = estimate.TeacherSharePct,
            TeacherEarnings = estimate.TeacherEarnings,
            PlatformShare = estimate.PlatformShare,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Task<decimal> ResolvePricePerHourAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        DateTime? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var at = asOf ?? DateTime.UtcNow;
        return ResolvePricePerHourInternalAsync(domainId, sessionTypeCode, marketCode, at, cancellationToken);
    }

    private async Task<decimal> ResolvePricePerHourInternalAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        DateTime asOf,
        CancellationToken cancellationToken)
    {
        var rate = await _priceRepository.GetEffectiveRateAsync(
            domainId, sessionTypeCode, marketCode, asOf, cancellationToken);
        if (rate == null)
            throw new InvalidOperationException(
                $"No active pricing rule for market '{marketCode}', domain {domainId} and session type '{sessionTypeCode}'.");
        return rate.PricePerHour;
    }

    private async Task<(decimal SharePct, int? LevelId)> ResolveTeacherShareAsync(
        int teacherId,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdWithLevelAsync(teacherId, cancellationToken);
        if (teacher == null)
            throw new InvalidOperationException($"Teacher {teacherId} not found.");

        if (teacher.CustomTeacherSharePct.HasValue)
            return (teacher.CustomTeacherSharePct.Value, teacher.TeacherLevelId);

        if (teacher.TeacherLevel == null)
            throw new InvalidOperationException($"Teacher {teacherId} has no assigned level.");

        return (teacher.TeacherLevel.TeacherSharePct, teacher.TeacherLevelId);
    }

    private static PriceEstimate BuildEstimate(
        DomainSessionPrice rate,
        int totalMinutes,
        decimal teacherSharePct,
        int? teacherLevelId,
        string currency)
    {
        var totalPrice = Math.Round((totalMinutes / 60m) * rate.PricePerHour, 2, MidpointRounding.AwayFromZero);
        var teacherEarnings = Math.Round(totalPrice * teacherSharePct / 100m, 2, MidpointRounding.AwayFromZero);
        var platformShare = totalPrice - teacherEarnings;

        return new PriceEstimate(
            rate.PricePerHour,
            totalMinutes,
            totalPrice,
            teacherSharePct,
            teacherEarnings,
            platformShare,
            rate.Id,
            teacherLevelId,
            rate.MarketCode,
            currency);
    }
}
