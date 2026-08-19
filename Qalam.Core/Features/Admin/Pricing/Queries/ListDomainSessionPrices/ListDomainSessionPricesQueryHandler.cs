using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListDomainSessionPrices;

public class ListDomainSessionPricesQueryHandler : ResponseHandler,
    IRequestHandler<ListDomainSessionPricesQuery, Response<List<DomainSessionPriceAdminDto>>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ListDomainSessionPricesQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<List<DomainSessionPriceAdminDto>>> Handle(
        ListDomainSessionPricesQuery request,
        CancellationToken cancellationToken)
    {
        var dtos = await _pricingAdminService.ListDomainSessionPricesAsync(
            request.DomainId,
            request.SessionTypeCode,
            request.IncludeHistory,
            cancellationToken);

        return Success(entity: dtos);
    }
}
