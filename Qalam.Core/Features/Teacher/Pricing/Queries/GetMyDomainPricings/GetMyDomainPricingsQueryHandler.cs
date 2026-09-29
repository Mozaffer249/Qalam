using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Pricing.Queries.GetMyDomainPricings;

public class GetMyDomainPricingsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyDomainPricingsQuery, Response<List<TeacherMyDomainPricingDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDomainPricingRepository _domainPricingRepository;
    private readonly IDomainSessionPriceRepository _priceRepository;
    private readonly IPricingMarketResolver _marketResolver;
    private readonly IPricingMarketRepository _marketRepository;
    private readonly ITeacherDomainApprovalRepository _approvalRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ITeacherLevelRepository _teacherLevelRepository;

    public GetMyDomainPricingsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDomainPricingRepository domainPricingRepository,
        IDomainSessionPriceRepository priceRepository,
        IPricingMarketResolver marketResolver,
        IPricingMarketRepository marketRepository,
        ITeacherDomainApprovalRepository approvalRepository,
        IEducationDomainRepository domainRepository,
        ITeacherLevelRepository teacherLevelRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _domainPricingRepository = domainPricingRepository;
        _priceRepository = priceRepository;
        _marketResolver = marketResolver;
        _marketRepository = marketRepository;
        _approvalRepository = approvalRepository;
        _domainRepository = domainRepository;
        _teacherLevelRepository = teacherLevelRepository;
    }

    public async Task<Response<List<TeacherMyDomainPricingDto>>> Handle(
        GetMyDomainPricingsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherMyDomainPricingDto>>("Teacher not found");

        var resolved = await _marketResolver.ResolveForUserAsync(request.UserId, cancellationToken);
        var market = await _marketRepository.GetByCodeAsync(resolved.MarketCode, cancellationToken);
        var fx = market is { ExchangeRateFromBase: > 0 } ? market.ExchangeRateFromBase : 1m;

        var rows = await _domainPricingRepository.ListByTeacherAsync(teacher.Id, cancellationToken);
        var approvals = await _approvalRepository.GetByTeacherAsync(teacher.Id, cancellationToken);
        var platformRates = await _priceRepository.ListCurrentRatesAsync(
            resolved.MarketCode, cancellationToken);

        var pricingByDomain = rows.ToDictionary(p => p.DomainId);
        var domainIds = rows.Select(p => p.DomainId)
            .Concat(approvals.Where(a => a.RevokedAt == null).Select(a => a.DomainId))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var starterLevel = rows.Count == domainIds.Count
                           && rows.All(p => p.CustomTeacherSharePct != null || p.TeacherLevel != null)
            ? null
            : await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);

        var result = new List<TeacherMyDomainPricingDto>(domainIds.Count);
        foreach (var domainId in domainIds)
        {
            pricingByDomain.TryGetValue(domainId, out var p);

            string? domainCode = p?.Domain?.Code;
            string? domainNameEn = p?.Domain?.NameEn;
            string? domainNameAr = p?.Domain?.NameAr;
            if (p?.Domain == null)
            {
                var domain = await _domainRepository.GetDomainDtoByIdAsync(domainId);
                domainCode = domain?.Code;
                domainNameEn = domain?.NameEn;
                domainNameAr = domain?.NameAr;
            }

            var individualPlatform = platformRates
                .FirstOrDefault(r =>
                    r.DomainId == domainId
                    && string.Equals(r.SessionTypeCode, PricingDefaults.SessionTypeIndividual, StringComparison.OrdinalIgnoreCase))
                ?.PricePerHour;
            var groupPlatform = platformRates
                .FirstOrDefault(r =>
                    r.DomainId == domainId
                    && string.Equals(r.SessionTypeCode, PricingDefaults.SessionTypeGroup, StringComparison.OrdinalIgnoreCase))
                ?.PricePerHour;

            decimal? customIndividual = p?.CustomIndividualPricePerHour is > 0
                ? PricingExchangeRateHelper.DeriveLocalPrice(p.CustomIndividualPricePerHour.Value, fx)
                : null;
            decimal? customGroup = p?.CustomGroupPricePerHour is > 0
                ? PricingExchangeRateHelper.DeriveLocalPrice(p.CustomGroupPricePerHour.Value, fx)
                : null;

            var share = ResolveShare(p, starterLevel);
            var effectiveShare = share ?? 0m;

            decimal? EarningsPerHour(decimal? custom, decimal? platform)
            {
                var basis = custom ?? platform;
                return basis.HasValue && share.HasValue
                    ? Math.Round(basis.Value * share.Value / 100m, 2, MidpointRounding.AwayFromZero)
                    : null;
            }

            var individualEarnings = EarningsPerHour(customIndividual, individualPlatform);
            var groupEarnings = EarningsPerHour(customGroup, groupPlatform);

            var hasBasis = (customIndividual ?? individualPlatform ?? customGroup ?? groupPlatform).HasValue;
            var priceStatus = individualEarnings.HasValue || groupEarnings.HasValue
                ? TeacherPriceStatus.Ready
                : !hasBasis
                    ? TeacherPriceStatus.NoPlatformRate
                    : TeacherPriceStatus.NoShare;

            result.Add(new TeacherMyDomainPricingDto
            {
                DomainId = domainId,
                DomainCode = domainCode,
                DomainNameEn = domainNameEn,
                DomainNameAr = domainNameAr,
                TeacherLevelId = p?.TeacherLevelId,
                TeacherLevelCode = p?.TeacherLevel?.Code,
                TeacherLevelNameEn = p?.TeacherLevel?.NameEn,
                TeacherLevelNameAr = p?.TeacherLevel?.NameAr,
                LevelSharePct = p?.TeacherLevel?.TeacherSharePct,
                CustomTeacherSharePct = p?.CustomTeacherSharePct,
                EffectiveSharePct = effectiveShare,
                PlatformIndividualPricePerHour = individualPlatform,
                PlatformGroupPricePerHour = groupPlatform,
                CustomIndividualPricePerHour = customIndividual,
                CustomGroupPricePerHour = customGroup,
                ReflectCustomIndividualPriceToStudent =
                    customIndividual.HasValue && p!.ReflectCustomIndividualPriceToStudent,
                ReflectCustomGroupPriceToStudent =
                    customGroup.HasValue && p!.ReflectCustomGroupPriceToStudent,
                IndividualTeacherEarningsPerHour = individualEarnings,
                GroupTeacherEarningsPerHour = groupEarnings,
                ProjectedSharePct = effectiveShare,
                PriceStatus = priceStatus,
                HasCompletedInterviewSession = teacher.HasCompletedInterviewSession,
                Currency = resolved.Currency,
                MarketCode = resolved.MarketCode,
            });
        }

        return Success(entity: result);
    }

    private static decimal? ResolveShare(TeacherDomainPricing? pricing, TeacherLevel? starterLevel)
    {
        if (pricing?.CustomTeacherSharePct.HasValue == true)
            return pricing.CustomTeacherSharePct.Value;
        if (pricing?.TeacherLevel != null)
            return pricing.TeacherLevel.TeacherSharePct;
        return starterLevel?.TeacherSharePct;
    }
}
