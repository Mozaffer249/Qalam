using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;

/// <summary>
/// Wire-level rules only. Catalog completeness (including identity when not yet submitted)
/// is enforced in the handler against active requirements + already-submitted codes.
/// </summary>
public class SubmitTeacherRegistrationRequirementsCommandValidator
    : AbstractValidator<SubmitTeacherRegistrationRequirementsCommand>
{
    public SubmitTeacherRegistrationRequirementsCommandValidator(
        IStringLocalizer<AuthenticationResources> localizer)
    {
        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .When(x => x.IdentityDocumentFile != null)
            .WithMessage(localizer[AuthenticationResourcesKeys.DocumentNumberRequired]);
    }
}
