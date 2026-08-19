using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.RejectLevelUpgradeSuggestion;

public class RejectLevelUpgradeSuggestionCommandHandler : ResponseHandler,
    IRequestHandler<RejectLevelUpgradeSuggestionCommand, Response<string>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public RejectLevelUpgradeSuggestionCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<string>> Handle(
        RejectLevelUpgradeSuggestionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _pricingAdminService.RejectLevelUpgradeSuggestionAsync(
                request.Id, request.Data?.ReviewNotes, cancellationToken);
            if (!success)
                return NotFound<string>("Suggestion not found.");

            return Success<string>("Teacher level upgrade rejected.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
