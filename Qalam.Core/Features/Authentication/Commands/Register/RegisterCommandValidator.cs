using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;

namespace Qalam.Core.Features.Authentication.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator(IStringLocalizer<AuthenticationResources> stringLocalizer)
        {
            RuleFor(x => x.FirstName)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.FirstNameIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.FirstNameIsRequired])
                .MaximumLength(100).WithMessage(stringLocalizer[AuthenticationResourcesKeys.FirstNameMaxLength])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.LastName)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.LastNameIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.LastNameIsRequired])
                .MaximumLength(100).WithMessage(stringLocalizer[AuthenticationResourcesKeys.LastNameMaxLength])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.Email)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.EmailIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.EmailIsRequired])
                .EmailAddress().WithMessage(stringLocalizer[AuthenticationResourcesKeys.EmailInvalidFormat])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.Password)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordIsRequired])
                .MinimumLength(6).WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordMinLength])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.ConfirmPassword)
                .NotNull().WithMessage(stringLocalizer[AuthenticationResourcesKeys.ConfirmPasswordIsRequired])
                .NotEmpty().WithMessage(stringLocalizer[AuthenticationResourcesKeys.ConfirmPasswordIsRequired])
                .Equal(x => x.Password).WithMessage(stringLocalizer[AuthenticationResourcesKeys.PasswordsDoNotMatch])
                .OverridePropertyName(string.Empty);
        }
    }
}

