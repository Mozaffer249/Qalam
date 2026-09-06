using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;

namespace Qalam.Core.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator(IStringLocalizer<AuthenticationResources> stringLocalizer)
        {
            RuleFor(x => x.UserNameOrEmail)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.UserNameOrEmailIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.UserNameOrEmailIsRequired])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.Password)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .MinimumLength(6).WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordMinLength])
                .OverridePropertyName(string.Empty);

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
}

