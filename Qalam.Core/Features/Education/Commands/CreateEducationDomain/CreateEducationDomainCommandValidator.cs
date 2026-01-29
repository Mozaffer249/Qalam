using FluentValidation;

namespace Qalam.Core.Features.Education.Commands.CreateEducationDomain;

public class CreateEducationDomainCommandValidator : AbstractValidator<CreateEducationDomainCommand>
{
    public CreateEducationDomainCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required")
            .MaximumLength(200).WithMessage("Arabic name cannot exceed 200 characters");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required")
            .MaximumLength(200).WithMessage("English name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters")
            .Matches("^[a-z0-9_]+$").WithMessage("Code must contain only lowercase letters, numbers and underscores");
    }
}
