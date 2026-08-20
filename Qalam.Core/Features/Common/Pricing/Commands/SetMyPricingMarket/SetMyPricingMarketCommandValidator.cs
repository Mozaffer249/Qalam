using FluentValidation;

namespace Qalam.Core.Features.Common.Pricing.Commands.SetMyPricingMarket;

public class SetMyPricingMarketCommandValidator : AbstractValidator<SetMyPricingMarketCommand>
{
    public SetMyPricingMarketCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        When(x => x.Data.MarketCode != null, () =>
        {
            RuleFor(x => x.Data.MarketCode).NotEmpty().MaximumLength(10);
        });
    }
}
