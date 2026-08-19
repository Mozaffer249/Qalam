using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.ApproveLevelUpgradeSuggestion;

public class ApproveLevelUpgradeSuggestionCommandValidator : AbstractValidator<ApproveLevelUpgradeSuggestionCommand>
{
    public ApproveLevelUpgradeSuggestionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
