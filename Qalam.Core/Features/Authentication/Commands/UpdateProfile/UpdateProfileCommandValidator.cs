using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;

namespace Qalam.Core.Features.Authentication.Commands.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        private readonly IStringLocalizer<AuthenticationResources> _localizer;

        public UpdateProfileCommandValidator(IStringLocalizer<AuthenticationResources> localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .WithMessage(_localizer[AuthenticationResourcesKeys.FirstNameMaxLength])
                .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

            RuleFor(x => x.LastName)
                .MaximumLength(100)
                .WithMessage(_localizer[AuthenticationResourcesKeys.LastNameMaxLength])
                .When(x => !string.IsNullOrWhiteSpace(x.LastName));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }
}

