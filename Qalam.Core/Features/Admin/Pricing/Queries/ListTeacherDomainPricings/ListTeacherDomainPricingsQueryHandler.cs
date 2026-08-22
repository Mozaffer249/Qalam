using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListTeacherDomainPricings;

public class ListTeacherDomainPricingsQueryHandler : ResponseHandler,
    IRequestHandler<ListTeacherDomainPricingsQuery, Response<List<TeacherDomainPricingAdminDto>>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ListTeacherDomainPricingsQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<List<TeacherDomainPricingAdminDto>>> Handle(
        ListTeacherDomainPricingsQuery request,
        CancellationToken cancellationToken)
    {
        var data = await _pricingAdminService.ListTeacherDomainPricingsAsync(
            request.DomainId, request.TeacherId, cancellationToken);
        return Success(entity: data);
    }
}
