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

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters")
            .Matches("^[a-z0-9_]+$").WithMessage("Code must contain only lowercase letters, numbers and underscores");
    }
}
