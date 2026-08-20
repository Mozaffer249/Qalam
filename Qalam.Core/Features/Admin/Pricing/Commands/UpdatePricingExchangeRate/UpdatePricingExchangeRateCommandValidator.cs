using FluentValidation;
using Qalam.Data.AppMetaData;

namespace Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingExchangeRate;

public class UpdatePricingExchangeRateCommandValidator : AbstractValidator<UpdatePricingExchangeRateCommand>
{
    public UpdatePricingExchangeRateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.ExchangeRateFromBase).GreaterThan(0);
        RuleFor(x => x.Code)
            .Must(code => code.Trim().ToLowerInvariant() != PricingMarketDefaults.DefaultMarketCode)
            .WithMessage("The base market exchange rate cannot be changed.");
    }
}
