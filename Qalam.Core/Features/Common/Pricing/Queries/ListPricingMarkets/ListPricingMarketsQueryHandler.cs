using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Common.Pricing.Queries.ListPricingMarkets;

public class ListPricingMarketsQueryHandler : ResponseHandler,
    IRequestHandler<ListPricingMarketsQuery, Response<List<PricingMarketDto>>>
{
    private readonly IPricingMarketService _pricingMarketService;

    public ListPricingMarketsQueryHandler(
        IPricingMarketService pricingMarketService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingMarketService = pricingMarketService;
    }

    public async Task<Response<List<PricingMarketDto>>> Handle(
        ListPricingMarketsQuery request,
        CancellationToken cancellationToken)
    {
        var markets = await _pricingMarketService.ListActiveMarketsAsync(cancellationToken);
        return Success(entity: markets);
    }
}
