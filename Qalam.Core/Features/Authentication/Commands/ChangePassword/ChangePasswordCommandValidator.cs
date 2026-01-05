using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;

namespace Qalam.Core.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator(IStringLocalizer<AuthenticationResources> localizer)
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(localizer[AuthenticationResourcesKeys.PasswordIsRequired]);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(localizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .MinimumLength(6).WithMessage(localizer[AuthenticationResourcesKeys.PasswordMinLength]);

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(localizer[AuthenticationResourcesKeys.ConfirmPasswordIsRequired])
                .Equal(x => x.NewPassword).WithMessage(localizer[AuthenticationResourcesKeys.PasswordsDoNotMatch]);
        }
    }
}


