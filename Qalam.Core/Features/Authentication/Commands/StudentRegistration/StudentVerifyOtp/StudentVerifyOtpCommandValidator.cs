using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentVerifyOtpCommandValidator : AbstractValidator<StudentVerifyOtpCommand>
{
    public StudentVerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.OtpCode).NotEmpty();

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
