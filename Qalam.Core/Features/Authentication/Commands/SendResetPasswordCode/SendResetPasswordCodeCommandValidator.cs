using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;

namespace Qalam.Core.Features.Authentication.Commands.SendResetPasswordCode
{
    public class SendResetPasswordCodeCommandValidator : AbstractValidator<SendResetPasswordCodeCommand>
    {
        public SendResetPasswordCodeCommandValidator(IStringLocalizer<AuthenticationResources> stringLocalizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.EmailIsRequired])
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.EmailIsRequired])
                .EmailAddress().WithMessage(stringLocalizer[AuthenticationResourcesKeys.EmailInvalidFormat])
                .OverridePropertyName(string.Empty);
        }
    }
}

