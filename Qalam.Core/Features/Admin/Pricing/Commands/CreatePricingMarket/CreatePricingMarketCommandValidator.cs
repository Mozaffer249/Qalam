using FluentValidation;
using System.Text.RegularExpressions;

namespace Qalam.Core.Features.Admin.Pricing.Commands.CreatePricingMarket;

public class CreatePricingMarketCommandValidator : AbstractValidator<CreatePricingMarketCommand>
{
    private static readonly Regex MarketCodePattern = new("^[a-z0-9]{2,10}$", RegexOptions.Compiled);
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

    public CreatePricingMarketCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .Must(c => MarketCodePattern.IsMatch(c.Trim().ToLowerInvariant()))
            .WithMessage("Market code must be 2–10 lowercase letters or digits.");
        RuleFor(x => x.Data.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Currency)
            .NotEmpty()
            .Must(c => CurrencyPattern.IsMatch(c.Trim().ToUpperInvariant()))
            .WithMessage("Currency must be a 3-letter ISO 4217 code.");
    }
}
