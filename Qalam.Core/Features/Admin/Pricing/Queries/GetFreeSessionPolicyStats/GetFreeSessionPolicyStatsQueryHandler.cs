using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.GetFreeSessionPolicyStats;

public class GetFreeSessionPolicyStatsQueryHandler : ResponseHandler,
    IRequestHandler<GetFreeSessionPolicyStatsQuery, Response<FreeSessionPolicyStatsDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public GetFreeSessionPolicyStatsQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<FreeSessionPolicyStatsDto>> Handle(
        GetFreeSessionPolicyStatsQuery request,
        CancellationToken cancellationToken)
    {
        var stats = await _pricingAdminService.GetFreeSessionPolicyStatsAsync(cancellationToken);
        return Success(entity: stats);
    }
}
