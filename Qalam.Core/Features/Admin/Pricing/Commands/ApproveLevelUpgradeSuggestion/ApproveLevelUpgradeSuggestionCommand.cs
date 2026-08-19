using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Admin.Pricing.Commands.ApproveLevelUpgradeSuggestion;

public class ApproveLevelUpgradeSuggestionCommand : IRequest<Response<string>>
{
    public int Id { get; set; }
    public ReviewLevelUpgradeSuggestionDto? Data { get; set; }
}
