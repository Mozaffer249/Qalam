using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Pricing;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetTargetedOsrEstimate;

public class GetTargetedOsrEstimateQueryHandler : ResponseHandler,
    IRequestHandler<GetTargetedOsrEstimateQuery, Response<PricingEstimateDto>>
{
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingMarketResolver _marketResolver;

    public GetTargetedOsrEstimateQueryHandler(
        IPricingEngine pricingEngine,
        IPricingMarketResolver marketResolver,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingEngine = pricingEngine;
        _marketResolver = marketResolver;
    }

    public async Task<Response<PricingEstimateDto>> Handle(
        GetTargetedOsrEstimateQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TeacherId <= 0)
            return BadRequest<PricingEstimateDto>("teacherId is required.");
        if (request.DomainId <= 0)
            return BadRequest<PricingEstimateDto>("domainId is required.");
        if (request.TotalMinutes <= 0)
            return BadRequest<PricingEstimateDto>("totalMinutes must be greater than zero.");

        var market = await _marketResolver.ResolveForUserAsync(request.UserId, cancellationToken);
        var sessionTypeCode = request.IsGroup ? "group" : "individual";

        var estimate = await _pricingEngine.EstimateAsync(new PricingEstimateRequest
        {
            DomainId = request.DomainId,
            SessionTypeCode = sessionTypeCode,
            MarketCode = market.MarketCode,
            TotalMinutes = request.TotalMinutes,
            TeacherId = request.TeacherId,
        }, cancellationToken);

        var dto = new PricingEstimateDto
        {
            PricePerHour = estimate.PricePerHour,
            Currency = estimate.Currency,
            MarketCode = estimate.MarketCode,
            TotalMinutes = estimate.TotalMinutes,
            TotalPrice = estimate.TotalPrice,
            TeacherSharePct = estimate.TeacherSharePct,
            TeacherEarnings = estimate.TeacherEarnings,
            PlatformShare = estimate.PlatformShare,
            EarningsPricePerHour = estimate.EarningsPricePerHour,
            ReflectCustomPriceToStudent = estimate.ReflectCustomPriceToStudent,
        };

        return Success(entity: dto);
    }
}
