using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevel;

public class SetTeacherLevelCommandHandler : ResponseHandler,
    IRequestHandler<SetTeacherLevelCommand, Response<string>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public SetTeacherLevelCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<string>> Handle(
        SetTeacherLevelCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _pricingAdminService.SetTeacherLevelAsync(
                request.TeacherId, request.Data, cancellationToken);
            if (!success)
                return NotFound<string>("Teacher not found.");

            return Success<string>("Teacher level updated.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
