using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevelTier;

public class SetTeacherLevelTierCommandValidator : AbstractValidator<SetTeacherLevelTierCommand>
{
    public SetTeacherLevelTierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.TeacherSharePct).InclusiveBetween(0, 100);
    }
}
