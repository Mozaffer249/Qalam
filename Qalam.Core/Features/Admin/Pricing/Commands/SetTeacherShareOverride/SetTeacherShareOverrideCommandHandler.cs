using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherShareOverride;

public class SetTeacherShareOverrideCommandHandler : ResponseHandler,
    IRequestHandler<SetTeacherShareOverrideCommand, Response<string>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public SetTeacherShareOverrideCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<string>> Handle(
        SetTeacherShareOverrideCommand request,
        CancellationToken cancellationToken)
    {
        var success = await _pricingAdminService.SetTeacherShareOverrideAsync(
            request.TeacherId, request.Data, cancellationToken);
        if (!success)
            return NotFound<string>("Teacher not found.");

        return Success<string>(request.Data.CustomTeacherSharePct.HasValue
            ? "Teacher share override set."
            : "Teacher share override cleared.");
    }
}
