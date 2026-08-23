using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;
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

    public GetMyDomainPricingsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDomainPricingRepository domainPricingRepository,
        IDomainSessionPriceRepository priceRepository,
        IPricingMarketResolver marketResolver,
        IPricingMarketRepository marketRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _domainPricingRepository = domainPricingRepository;
        _priceRepository = priceRepository;
        _marketResolver = marketResolver;
        _marketRepository = marketRepository;
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
        var platformRates = await _priceRepository.ListCurrentRatesAsync(
            resolved.MarketCode, cancellationToken);

        var result = rows.Select(p =>
        {
            var individualPlatform = platformRates
                .FirstOrDefault(r =>
                    r.DomainId == p.DomainId
                    && string.Equals(r.SessionTypeCode, PricingDefaults.SessionTypeIndividual, StringComparison.OrdinalIgnoreCase))
                ?.PricePerHour;
            var groupPlatform = platformRates
                .FirstOrDefault(r =>
                    r.DomainId == p.DomainId
                    && string.Equals(r.SessionTypeCode, PricingDefaults.SessionTypeGroup, StringComparison.OrdinalIgnoreCase))
                ?.PricePerHour;

            decimal? customIndividual = p.CustomIndividualPricePerHour is > 0
                ? PricingExchangeRateHelper.DeriveLocalPrice(p.CustomIndividualPricePerHour.Value, fx)
                : null;
            decimal? customGroup = p.CustomGroupPricePerHour is > 0
                ? PricingExchangeRateHelper.DeriveLocalPrice(p.CustomGroupPricePerHour.Value, fx)
                : null;

            var effectiveShare = p.CustomTeacherSharePct
                ?? (p.HasCompletedInterviewSession && p.TeacherLevel != null
                    ? p.TeacherLevel.TeacherSharePct
                    : 0m);

            return new TeacherMyDomainPricingDto
            {
                DomainId = p.DomainId,
                DomainCode = p.Domain?.Code,
                DomainNameEn = p.Domain?.NameEn,
                DomainNameAr = p.Domain?.NameAr,
                TeacherLevelId = p.TeacherLevelId,
                TeacherLevelCode = p.TeacherLevel?.Code,
                TeacherLevelNameEn = p.TeacherLevel?.NameEn,
                TeacherLevelNameAr = p.TeacherLevel?.NameAr,
                LevelSharePct = p.TeacherLevel?.TeacherSharePct,
                CustomTeacherSharePct = p.CustomTeacherSharePct,
                EffectiveSharePct = effectiveShare,
                PlatformIndividualPricePerHour = individualPlatform,
                PlatformGroupPricePerHour = groupPlatform,
                CustomIndividualPricePerHour = customIndividual,
                CustomGroupPricePerHour = customGroup,
                ReflectCustomIndividualPriceToStudent =
                    customIndividual.HasValue && p.ReflectCustomIndividualPriceToStudent,
                ReflectCustomGroupPriceToStudent =
                    customGroup.HasValue && p.ReflectCustomGroupPriceToStudent,
                HasCompletedInterviewSession = p.HasCompletedInterviewSession,
                Currency = resolved.Currency,
                MarketCode = resolved.MarketCode,
            };
        }).ToList();

        return Success(entity: result);
    }
}
