using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Queries.ListLevelUpgradeSuggestions;

public class ListLevelUpgradeSuggestionsQuery : IRequest<Response<List<TeacherLevelUpgradeSuggestionAdminDto>>>
{
    public string Status { get; set; } = "Pending";
}
