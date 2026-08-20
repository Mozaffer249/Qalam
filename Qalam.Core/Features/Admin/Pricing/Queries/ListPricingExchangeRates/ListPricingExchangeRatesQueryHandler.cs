using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListPricingExchangeRates;

public class ListPricingExchangeRatesQueryHandler : ResponseHandler,
    IRequestHandler<ListPricingExchangeRatesQuery, Response<List<PricingExchangeRateAdminDto>>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ListPricingExchangeRatesQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<List<PricingExchangeRateAdminDto>>> Handle(
        ListPricingExchangeRatesQuery request,
        CancellationToken cancellationToken)
    {
        var rates = await _pricingAdminService.ListPricingExchangeRatesAsync(cancellationToken);
        return Success(entity: rates);
    }
}
