using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListLevelUpgradeSuggestions;

public class ListLevelUpgradeSuggestionsQueryHandler : ResponseHandler,
    IRequestHandler<ListLevelUpgradeSuggestionsQuery, Response<List<TeacherLevelUpgradeSuggestionAdminDto>>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public ListLevelUpgradeSuggestionsQueryHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<List<TeacherLevelUpgradeSuggestionAdminDto>>> Handle(
        ListLevelUpgradeSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtos = await _pricingAdminService.ListLevelUpgradeSuggestionsAsync(
                request.Status, cancellationToken);
            return Success(entity: dtos);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<List<TeacherLevelUpgradeSuggestionAdminDto>>(ex.Message);
        }
    }
}
