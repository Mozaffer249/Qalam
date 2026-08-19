using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.ApproveLevelUpgradeSuggestion;

public class ApproveLevelUpgradeSuggestionCommandHandler : ResponseHandler,
    IRequestHandler<ApproveLevelUpgradeSuggestionCommand, Response<string>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ApproveLevelUpgradeSuggestionCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<string>> Handle(
        ApproveLevelUpgradeSuggestionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _pricingAdminService.ApproveLevelUpgradeSuggestionAsync(
                request.Id, request.Data?.ReviewNotes, cancellationToken);
            if (!success)
                return NotFound<string>("Suggestion not found.");

            return Success<string>("Teacher level upgrade approved.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
