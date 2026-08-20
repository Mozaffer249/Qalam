using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDefaultPricingMarket;

public class SetDefaultPricingMarketCommandValidator : AbstractValidator<SetDefaultPricingMarketCommand>
{
    public SetDefaultPricingMarketCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
    }
}
