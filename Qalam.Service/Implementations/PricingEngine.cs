using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Implementations;

public class PricingEngine : IPricingEngine
{
    private readonly IDomainSessionPriceRepository _priceRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDomainPricingRepository _domainPricingRepository;
    private readonly IPricingMarketRepository _marketRepository;

    public PricingEngine(
        IDomainSessionPriceRepository priceRepository,
        ITeacherRepository teacherRepository,
        ITeacherDomainPricingRepository domainPricingRepository,
        IPricingMarketRepository marketRepository)
    {
        _priceRepository = priceRepository;
        _teacherRepository = teacherRepository;
        _domainPricingRepository = domainPricingRepository;
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

        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher == null)
            throw new InvalidOperationException($"Teacher {request.TeacherId} not found.");

        var domainPricing = await _domainPricingRepository.GetByTeacherAndDomainAsync(
            request.TeacherId,
            request.DomainId,
            cancellationToken);

        var share = ResolveTeacherShare(domainPricing);
        var platformPricePerHour = rate.PricePerHour;
        var isGroup = string.Equals(
            request.SessionTypeCode,
            PricingDefaults.SessionTypeGroup,
            StringComparison.OrdinalIgnoreCase);
        var customLocal = ResolveCustomPriceInMarket(domainPricing, market, isGroup);
        var reflect = ResolveReflectCustomPriceToStudent(domainPricing, isGroup);

        var studentPricePerHour = reflect && customLocal.HasValue ? customLocal.Value : platformPricePerHour;
        var earningsPricePerHour = customLocal ?? platformPricePerHour;

        return BuildEstimate(
            studentPricePerHour,
            earningsPricePerHour,
            request.TotalMinutes,
            share.SharePct,
            share.LevelId,
            rate.Id,
            market.Code,
            market.Currency,
            reflect);
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
            ReflectCustomPriceToStudent = estimate.ReflectCustomPriceToStudent,
            EarningsPricePerHour = estimate.EarningsPricePerHour,
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

    /// <summary>
    /// Teacher earnings for <paramref name="minutes"/> using the projected share
    /// (<paramref name="customSharePct"/> → <paramref name="levelSharePct"/> → the estimate's effective share),
    /// so teachers still see their future earnings while the domain interview is pending.
    /// </summary>
    public static TeacherEarningsProjection ProjectTeacherEarnings(
        PriceEstimate estimate,
        decimal? customSharePct,
        decimal? levelSharePct,
        int minutes)
    {
        var sharePct = customSharePct ?? levelSharePct ?? estimate.TeacherSharePct;
        var earningsHourly = estimate.EarningsPricePerHour ?? estimate.PricePerHour;
        var perHour = Math.Round(earningsHourly * sharePct / 100m, 2, MidpointRounding.AwayFromZero);
        var total = minutes > 0
            ? Math.Round(earningsHourly * (sharePct / 100m) * (minutes / 60m), 2, MidpointRounding.AwayFromZero)
            : 0m;
        return new TeacherEarningsProjection(sharePct, perHour, total);
    }

    public static TeacherEarningsProjection ProjectTeacherEarnings(
        PriceEstimate estimate,
        TeacherDomainPricing? domainPricing,
        int minutes)
        => ProjectTeacherEarnings(
            estimate,
            domainPricing?.CustomTeacherSharePct,
            domainPricing?.TeacherLevel?.TeacherSharePct,
            minutes);

    /// <summary>
    /// Teacher earnings frozen on a snapshot; when the snapshot was taken at 0% (interview pending)
    /// the projected share is applied to the snapshot's earnings base instead.
    /// </summary>
    public static decimal ProjectTeacherEarnings(PricingSnapshot snapshot, TeacherDomainPricing? domainPricing)
    {
        if (snapshot.TeacherSharePct > 0m)
            return snapshot.TeacherEarnings;

        var sharePct = domainPricing?.CustomTeacherSharePct ?? domainPricing?.TeacherLevel?.TeacherSharePct ?? 0m;
        var earningsHourly = snapshot.EarningsPricePerHour ?? snapshot.PricePerHour;
        return Math.Round(
            earningsHourly * (sharePct / 100m) * (snapshot.TotalMinutes / 60m),
            2,
            MidpointRounding.AwayFromZero);
    }

    private static (decimal SharePct, int? LevelId) ResolveTeacherShare(TeacherDomainPricing? pricing)
    {
        if (pricing?.CustomTeacherSharePct.HasValue == true)
            return (pricing.CustomTeacherSharePct.Value, pricing.TeacherLevelId);

        // Interview / probation for this domain: unpaid until unlocked with a level.
        if (pricing == null
            || !pricing.HasCompletedInterviewSession
            || pricing.TeacherLevel == null)
            return (0m, pricing?.TeacherLevelId);

        return (pricing.TeacherLevel.TeacherSharePct, pricing.TeacherLevelId);
    }

    private static decimal? ResolveCustomPriceInMarket(
        TeacherDomainPricing? pricing,
        PricingMarket market,
        bool isGroup)
    {
        var customBase = isGroup
            ? pricing?.CustomGroupPricePerHour
            : pricing?.CustomIndividualPricePerHour;
        if (customBase is not > 0)
            return null;

        var fx = market.ExchangeRateFromBase > 0 ? market.ExchangeRateFromBase : 1m;
        return PricingExchangeRateHelper.DeriveLocalPrice(customBase.Value, fx);
    }

    private static bool ResolveReflectCustomPriceToStudent(TeacherDomainPricing? pricing, bool isGroup)
    {
        if (pricing == null)
            return false;

        if (isGroup)
            return pricing.CustomGroupPricePerHour is > 0 && pricing.ReflectCustomGroupPriceToStudent;

        return pricing.CustomIndividualPricePerHour is > 0 && pricing.ReflectCustomIndividualPriceToStudent;
    }

    private static PriceEstimate BuildEstimate(
        decimal studentPricePerHour,
        decimal earningsPricePerHour,
        int totalMinutes,
        decimal teacherSharePct,
        int? teacherLevelId,
        int? domainSessionPriceId,
        string marketCode,
        string currency,
        bool reflectedCustomPrice)
    {
        var hours = totalMinutes / 60m;
        var totalPrice = Math.Round(hours * studentPricePerHour, 2, MidpointRounding.AwayFromZero);
        var earningsBase = Math.Round(hours * earningsPricePerHour, 2, MidpointRounding.AwayFromZero);
        var teacherEarnings = Math.Round(earningsBase * teacherSharePct / 100m, 2, MidpointRounding.AwayFromZero);
        var platformShare = totalPrice - teacherEarnings;

        return new PriceEstimate(
            studentPricePerHour,
            totalMinutes,
            totalPrice,
            teacherSharePct,
            teacherEarnings,
            platformShare,
            domainSessionPriceId,
            teacherLevelId,
            marketCode,
            currency,
            reflectedCustomPrice,
            earningsPricePerHour);
    }
}
