using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.BackfillStarterTeacherLevels;

public class BackfillStarterTeacherLevelsCommandHandler : ResponseHandler,
    IRequestHandler<BackfillStarterTeacherLevelsCommand, Response<BackfillStarterTeacherLevelsResultDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public BackfillStarterTeacherLevelsCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<BackfillStarterTeacherLevelsResultDto>> Handle(
        BackfillStarterTeacherLevelsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _pricingAdminService.BackfillStarterTeacherLevelsAsync(cancellationToken);
        return Success(
            $"Assigned starter level to {result.UpdatedCount} teacher(s).",
            entity: result);
    }
}
