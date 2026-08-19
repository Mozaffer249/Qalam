using FluentValidation;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevel;

public class SetTeacherLevelCommandValidator : AbstractValidator<SetTeacherLevelCommand>
{
    public SetTeacherLevelCommandValidator()
    {
        RuleFor(x => x.TeacherId).GreaterThan(0);
        RuleFor(x => x.Data.TeacherLevelId).GreaterThan(0);
    }
}
