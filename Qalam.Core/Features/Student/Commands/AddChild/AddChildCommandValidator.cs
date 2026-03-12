using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.Commands.AddChild;

public class AddChildCommandValidator : AbstractValidator<AddChildCommand>
{
    public AddChildCommandValidator()
    {
        RuleFor(x => x.Child).NotNull();

        When(x => x.Child != null, () =>
        {
            RuleFor(x => x.Child.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(x => x.Child.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(x => x.Child.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.Child.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.Child.Password).WithMessage("Passwords do not match.");

            RuleFor(x => x.Child.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required.")
                .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Date of birth cannot be in the future.")
                .Must(dob => dob > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
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

            // Hierarchy: CurriculumId requires DomainId, LevelId requires CurriculumId, GradeId requires LevelId
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
