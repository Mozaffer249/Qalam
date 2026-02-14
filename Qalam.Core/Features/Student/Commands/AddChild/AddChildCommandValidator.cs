using FluentValidation;

namespace Qalam.Core.Features.Student.Commands.AddChild;

public class AddChildCommandValidator : AbstractValidator<AddChildCommand>
{
    public AddChildCommandValidator()
    {
        RuleFor(x => x.Child).NotNull();
        RuleFor(x => x.Child.FullName).NotEmpty().When(x => x.Child != null);
        RuleFor(x => x.Child.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => x.Child != null);
        RuleFor(x => x.Child.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .When(x => x.Child != null);
        RuleFor(x => x.Child.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Child.Password).WithMessage("Passwords do not match.")
            .When(x => x.Child != null);
    }
}
