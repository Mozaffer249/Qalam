using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListTeacherLevelTiers;

public class ListTeacherLevelTiersQueryHandler : ResponseHandler,
    IRequestHandler<ListTeacherLevelTiersQuery, Response<List<TeacherLevelTierAdminDto>>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ListTeacherLevelTiersQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<List<TeacherLevelTierAdminDto>>> Handle(
        ListTeacherLevelTiersQuery request,
        CancellationToken cancellationToken)
    {
        var dtos = await _pricingAdminService.ListTeacherLevelTiersAsync(cancellationToken);
        return Success(entity: dtos);
    }
}
