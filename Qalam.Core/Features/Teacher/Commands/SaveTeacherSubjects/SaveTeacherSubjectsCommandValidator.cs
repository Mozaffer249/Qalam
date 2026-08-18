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

            subject.When(s => !s.CanTeachFullSubject, () =>
            {
                subject.RuleFor(s => s.Units)
                    .NotEmpty()
                    .WithMessage("Units are required when CanTeachFullSubject is false");
            });

            subject.RuleForEach(s => s.Units).ChildRules(unit =>
            {
                unit.RuleFor(u => u.UnitId)
                    .GreaterThan(0)
                    .WithMessage("Unit ID must be greater than 0");
            });

            subject.RuleForEach(s => s.QuranContentTypeIds)
                .GreaterThan(0)
                .WithMessage("Quran content type ID must be greater than 0");

            subject.RuleForEach(s => s.QuranLevelIds)
                .GreaterThan(0)
                .WithMessage("Quran level ID must be greater than 0");

            subject.RuleForEach(s => s.EducationLevelIds)
                .GreaterThan(0)
                .WithMessage("Education level ID must be greater than 0");

            subject.RuleForEach(s => s.GradeIds)
                .GreaterThan(0)
                .WithMessage("Grade ID must be greater than 0");
        });
    }
}
