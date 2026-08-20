using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetDomainSessionPrice;

public class SetDomainSessionPriceCommandValidator : AbstractValidator<SetDomainSessionPriceCommand>
{
    public SetDomainSessionPriceCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.MarketCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Data.DomainId).GreaterThan(0);
        RuleFor(x => x.Data.SessionTypeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Data.PricePerHour).GreaterThan(0);
    }
}
