using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListPricingMarkets;

public class ListAdminPricingMarketsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminPricingMarketsQuery, Response<List<PricingMarketDto>>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ListAdminPricingMarketsQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<List<PricingMarketDto>>> Handle(
        ListAdminPricingMarketsQuery request,
        CancellationToken cancellationToken)
    {
        var markets = await _pricingAdminService.ListPricingMarketsAsync(cancellationToken);
        return Success(entity: markets);
    }
}
