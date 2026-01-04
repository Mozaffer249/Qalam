using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Shared;

namespace Qalam.Core.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator(IStringLocalizer<SharedResources> stringLocalizer)
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage(stringLocalizer[SharedResourcesKeys.IsRequired])
                .NotNull().WithMessage(stringLocalizer[SharedResourcesKeys.IsRequired])
                .OverridePropertyName(string.Empty);

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage(stringLocalizer[SharedResourcesKeys.IsRequired])
                .NotNull().WithMessage(stringLocalizer[SharedResourcesKeys.IsRequired])
                .OverridePropertyName(string.Empty);
        }
    }
}

