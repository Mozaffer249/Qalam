using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.RegisterDeviceToken;

public class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    private static readonly string[] AllowedPlatforms = ["ios", "android", "web"];

    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .MaximumLength(512);

        RuleFor(x => x.Platform)
            .NotEmpty().WithMessage("Platform is required")
            .Must(p => AllowedPlatforms.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Platform must be ios, android, or web");

        RuleFor(x => x.AppVersion)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.AppVersion));
    }
}
