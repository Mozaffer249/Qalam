using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommandValidator : AbstractValidator<VerifyOtpAndCreateAccountCommand>
{
    public VerifyOtpAndCreateAccountCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required")
            .Length(4).WithMessage("OTP code must be 4 digits");

        RuleFor(x => x.DeviceToken)
            .MaximumLength(512)
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceToken));

        RuleFor(x => x.DeviceTokenPlatform)
            .Must(p => string.IsNullOrWhiteSpace(p)
                       || new[] { "ios", "android", "web" }.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("DeviceTokenPlatform must be ios, android, or web")
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceToken));

        RuleFor(x => x.AppVersion)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.AppVersion));
    }
}
