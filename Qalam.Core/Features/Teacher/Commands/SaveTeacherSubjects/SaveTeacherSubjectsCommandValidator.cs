using FluentValidation;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherSubjects;

public class SaveTeacherSubjectsCommandValidator : AbstractValidator<SaveTeacherSubjectsCommand>
{
    public SaveTeacherSubjectsCommandValidator()
    {
        RuleFor(x => x.Subjects)
            .NotNull()
            .WithMessage("Subjects list is required");

        RuleForEach(x => x.Subjects).ChildRules(subject =>
        {
            subject.RuleFor(s => s.SubjectId)
                .GreaterThan(0)
                .WithMessage("Subject ID must be greater than 0");

            // When CanTeachFullSubject is false, units are required
            subject.When(s => !s.CanTeachFullSubject, () =>
            {
                subject.RuleFor(s => s.Units)
                    .NotEmpty()
                    .WithMessage("Units are required when CanTeachFullSubject is false");
            });

            // Validate each unit
            subject.RuleForEach(s => s.Units).ChildRules(unit =>
            {
                unit.RuleFor(u => u.UnitId)
                    .GreaterThan(0)
                    .WithMessage("Unit ID must be greater than 0");

                // QuranContentTypeId validation (if provided, must be valid range)
                unit.When(u => u.QuranContentTypeId.HasValue, () =>
                {
                    unit.RuleFor(u => u.QuranContentTypeId)
                        .InclusiveBetween(1, 10)
                        .WithMessage("Invalid Quran Content Type ID");
                });

                // QuranLevelId validation (if provided, must be valid range)
                unit.When(u => u.QuranLevelId.HasValue, () =>
                {
                    unit.RuleFor(u => u.QuranLevelId)
                        .InclusiveBetween(1, 10)
                        .WithMessage("Invalid Quran Level ID");
                });
            });
        });
    }
}
