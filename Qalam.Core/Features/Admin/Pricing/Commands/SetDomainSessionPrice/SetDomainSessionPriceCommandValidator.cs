using FluentValidation;
using Qalam.Data.AppMetaData;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDomainSessionPrice;

public class SetDomainSessionPriceCommandValidator : AbstractValidator<SetDomainSessionPriceCommand>
{
    public SetDomainSessionPriceCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.DomainId).GreaterThan(0);
        RuleFor(x => x.Data.SessionTypeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Data.PricePerHour).GreaterThan(0);
        When(x => !string.IsNullOrWhiteSpace(x.Data.MarketCode), () =>
        {
            RuleFor(x => x.Data.MarketCode!)
                .Must(code => code.Trim().ToLowerInvariant() == PricingMarketDefaults.DefaultMarketCode)
                .WithMessage("Domain rates must be set in the base market (SAR).");
        });
    }
}
