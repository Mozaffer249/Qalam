using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Common.Pricing.Queries.GetMyPricingMarket;

public class GetMyPricingMarketQueryHandler : ResponseHandler,
    IRequestHandler<GetMyPricingMarketQuery, Response<MyPricingMarketDto>>
{
    private readonly IPricingMarketService _pricingMarketService;

    public GetMyPricingMarketQueryHandler(
        IPricingMarketService pricingMarketService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingMarketService = pricingMarketService;
    }

    public async Task<Response<MyPricingMarketDto>> Handle(
        GetMyPricingMarketQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _pricingMarketService.GetMyMarketAsync(request.UserId, cancellationToken);
        return Success(entity: dto);
    }
}
