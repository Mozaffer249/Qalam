using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevelTier;

public class SetTeacherLevelTierCommandHandler : ResponseHandler,
    IRequestHandler<SetTeacherLevelTierCommand, Response<TeacherLevelTierAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public SetTeacherLevelTierCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<TeacherLevelTierAdminDto>> Handle(
        SetTeacherLevelTierCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _pricingAdminService.SetTeacherLevelTierAsync(
            request.Id, request.Data, cancellationToken);
        if (result == null)
            return NotFound<TeacherLevelTierAdminDto>("Teacher level not found.");

        return Success(entity: result);
    }
}
