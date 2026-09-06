using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.DeactivateAccount;

public class DeactivateAccountCommandValidator : AbstractValidator<DeactivateAccountCommand>
{
    public DeactivateAccountCommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
