using FluentValidation;

namespace Qalam.Core.Features.Education.Commands.UpdateEducationDomain;

public class UpdateEducationDomainCommandValidator : AbstractValidator<UpdateEducationDomainCommand>
{
    public UpdateEducationDomainCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required")
            .MaximumLength(200).WithMessage("Arabic name cannot exceed 200 characters");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required")
            .MaximumLength(200).WithMessage("English name cannot exceed 200 characters");

        RuleFor(x => x.ArabicCode)
            .NotEmpty().WithMessage("Arabic code is required")
            .MaximumLength(50).WithMessage("Arabic code cannot exceed 50 characters");

        RuleFor(x => x.EnglishCode)
            .NotEmpty().WithMessage("English code is required")
            .MaximumLength(50).WithMessage("English code cannot exceed 50 characters")
            .Matches("^[a-z0-9_]+$").WithMessage("English code must contain only lowercase letters, numbers and underscores");
    }
}
