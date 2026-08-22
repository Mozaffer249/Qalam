using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherShareOverride;

public class SetTeacherShareOverrideCommandValidator : AbstractValidator<SetTeacherShareOverrideCommand>
{
    public SetTeacherShareOverrideCommandValidator()
    {
        RuleFor(x => x.TeacherId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.DomainId).GreaterThan(0);
        RuleFor(x => x.Data.CustomTeacherSharePct)
            .InclusiveBetween(0, 100)
            .When(x => x.Data.CustomTeacherSharePct.HasValue);
    }
}
