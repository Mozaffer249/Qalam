using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherDomainPricing;

public class SetTeacherDomainPricingCommandValidator : AbstractValidator<SetTeacherDomainPricingCommand>
{
    public SetTeacherDomainPricingCommandValidator()
    {
        RuleFor(x => x.TeacherId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.DomainId).GreaterThan(0);
    }
}
