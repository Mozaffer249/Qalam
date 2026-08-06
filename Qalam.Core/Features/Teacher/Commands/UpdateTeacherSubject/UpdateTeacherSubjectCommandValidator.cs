using FluentValidation;

namespace Qalam.Core.Features.Teacher.Commands.UpdateTeacherSubject;

public class UpdateTeacherSubjectCommandValidator : AbstractValidator<UpdateTeacherSubjectCommand>
{
    public UpdateTeacherSubjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Teacher subject ID must be greater than 0");

        When(x => !x.CanTeachFullSubject, () =>
        {
            RuleFor(x => x.Units)
                .NotEmpty()
                .WithMessage("Units are required when CanTeachFullSubject is false");
        });

        RuleForEach(x => x.Units).ChildRules(unit =>
        {
            unit.RuleFor(u => u.UnitId)
                .GreaterThan(0)
                .WithMessage("Unit ID must be greater than 0");
        });

        RuleForEach(x => x.QuranContentTypeIds)
            .GreaterThan(0)
            .WithMessage("Quran content type ID must be greater than 0");

        RuleForEach(x => x.QuranLevelIds)
            .GreaterThan(0)
            .WithMessage("Quran level ID must be greater than 0");
    }
}
