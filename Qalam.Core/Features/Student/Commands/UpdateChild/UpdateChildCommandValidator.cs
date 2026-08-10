using FluentValidation;

namespace Qalam.Core.Features.Student.Commands.UpdateChild;

public class UpdateChildCommandValidator : AbstractValidator<UpdateChildCommand>
{
    public UpdateChildCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a positive number.");

        RuleFor(x => x.Child).NotNull();

        When(x => x.Child != null, () =>
        {
            RuleFor(x => x.Child.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(x => x.Child.DateOfBirth)
                .Must(dob => !dob.HasValue || dob.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Date of birth cannot be in the future.")
                .Must(dob => !dob.HasValue || dob.Value > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
                    .WithMessage("Child must be under 18 years old.");

            RuleFor(x => x.Child.Gender)
                .IsInEnum().WithMessage("Invalid gender value.")
                .When(x => x.Child.Gender.HasValue);

            RuleFor(x => x.Child.GuardianRelation)
                .IsInEnum().WithMessage("Invalid guardian relation value.")
                .When(x => x.Child.GuardianRelation.HasValue);

            RuleFor(x => x.Child.DomainId)
                .GreaterThan(0).WithMessage("DomainId must be a positive number.")
                .When(x => x.Child.DomainId.HasValue);

            RuleFor(x => x.Child.CurriculumId)
                .GreaterThan(0).WithMessage("CurriculumId must be a positive number.")
                .When(x => x.Child.CurriculumId.HasValue);

            RuleFor(x => x.Child.LevelId)
                .GreaterThan(0).WithMessage("LevelId must be a positive number.")
                .When(x => x.Child.LevelId.HasValue);

            RuleFor(x => x.Child.GradeId)
                .GreaterThan(0).WithMessage("GradeId must be a positive number.")
                .When(x => x.Child.GradeId.HasValue);

            RuleFor(x => x.Child.DomainId)
                .NotNull().WithMessage("DomainId is required when CurriculumId is specified.")
                .When(x => x.Child.CurriculumId.HasValue);

            RuleFor(x => x.Child.CurriculumId)
                .NotNull().WithMessage("CurriculumId is required when LevelId is specified.")
                .When(x => x.Child.LevelId.HasValue);

            RuleFor(x => x.Child.LevelId)
                .NotNull().WithMessage("LevelId is required when GradeId is specified.")
                .When(x => x.Child.GradeId.HasValue);
        });
    }
}
