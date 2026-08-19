using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.RejectLevelUpgradeSuggestion;

public class RejectLevelUpgradeSuggestionCommandValidator : AbstractValidator<RejectLevelUpgradeSuggestionCommand>
{
    public RejectLevelUpgradeSuggestionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
