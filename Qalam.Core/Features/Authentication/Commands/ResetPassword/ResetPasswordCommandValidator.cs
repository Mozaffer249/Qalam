using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;

namespace Qalam.Core.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator(
            IStringLocalizer<AuthenticationResources> authLocalizer,
            IStringLocalizer<SharedResources> sharedLocalizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(authLocalizer[AuthenticationResourcesKeys.EmailIsRequired])
                .NotNull().WithMessage(authLocalizer[AuthenticationResourcesKeys.EmailIsRequired])
                .EmailAddress().WithMessage(authLocalizer[AuthenticationResourcesKeys.EmailInvalidFormat])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.ResetCode)
                .NotEmpty().WithMessage("Reset code is required.")
                .Length(6).WithMessage("Reset code must be exactly 6 digits.")
                .Matches(@"^\d{6}$").WithMessage("Reset code must contain only numbers.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(authLocalizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .NotNull().WithMessage(authLocalizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .MinimumLength(6).WithMessage(authLocalizer[AuthenticationResourcesKeys.PasswordMinLength])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(authLocalizer[AuthenticationResourcesKeys.ConfirmPasswordIsRequired])
                .NotNull().WithMessage(authLocalizer[AuthenticationResourcesKeys.ConfirmPasswordIsRequired])
                .Equal(x => x.NewPassword).WithMessage(authLocalizer[AuthenticationResourcesKeys.PasswordsDoNotMatch])
                .OverridePropertyName(string.Empty);
        }
    }
}


