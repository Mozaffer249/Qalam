using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.UnregisterDeviceToken;

public class UnregisterDeviceTokenCommandValidator : AbstractValidator<UnregisterDeviceTokenCommand>
{
    public UnregisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .MaximumLength(512);
    }
}
