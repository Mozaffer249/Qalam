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

            unit.When(u => u.QuranContentTypeId.HasValue, () =>
            {
                unit.RuleFor(u => u.QuranContentTypeId)
                    .InclusiveBetween(1, 10)
                    .WithMessage("Invalid Quran Content Type ID");
            });

            unit.When(u => u.QuranLevelId.HasValue, () =>
            {
                unit.RuleFor(u => u.QuranLevelId)
                    .InclusiveBetween(1, 10)
                    .WithMessage("Invalid Quran Level ID");
            });
        });
    }
}
