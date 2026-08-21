using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.CreateTeacherLevelTier;

public class CreateTeacherLevelTierCommandHandler : ResponseHandler,
    IRequestHandler<CreateTeacherLevelTierCommand, Response<TeacherLevelTierAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public CreateTeacherLevelTierCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<TeacherLevelTierAdminDto>> Handle(
        CreateTeacherLevelTierCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.CreateTeacherLevelTierAsync(
                request.Data,
                cancellationToken);
            return Success("Teacher level created.", entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<TeacherLevelTierAdminDto>(ex.Message);
        }
    }
}
