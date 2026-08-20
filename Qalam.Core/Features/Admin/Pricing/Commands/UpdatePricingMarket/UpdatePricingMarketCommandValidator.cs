using FluentValidation;
using System.Text.RegularExpressions;

namespace Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingMarket;

public class UpdatePricingMarketCommandValidator : AbstractValidator<UpdatePricingMarketCommand>
{
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

    public UpdatePricingMarketCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Currency)
            .NotEmpty()
            .Must(c => CurrencyPattern.IsMatch(c.Trim().ToUpperInvariant()))
            .WithMessage("Currency must be a 3-letter ISO 4217 code.");
    }
}
