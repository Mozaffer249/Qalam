using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

/// <summary>
/// Light validation — required fields are enforced by active registration requirements in the submit handler.
/// </summary>
public class UploadTeacherDocumentsCommandValidator : AbstractValidator<UploadTeacherDocumentsCommand>
{
    public UploadTeacherDocumentsCommandValidator(IStringLocalizer<AuthenticationResources> localizer)
    {
        RuleForEach(x => x.Certificates)
            .ChildRules(cert =>
            {
                cert.RuleFor(c => c.File)
                    .NotNull()
                    .When(c => c != null)
                    .WithMessage(localizer[AuthenticationResourcesKeys.CertificateFileRequired]);
            })
            .When(x => x.Certificates != null && x.Certificates.Count > 0);

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .When(x => x.IdentityDocumentFile != null)
            .WithMessage(localizer[AuthenticationResourcesKeys.DocumentNumberRequired]);

        RuleFor(x => x.IssuingCountryCode)
            .NotEmpty()
            .When(x => x.IdentityDocumentFile != null &&
                      (x.IdentityType == IdentityType.Passport || x.IdentityType == IdentityType.DrivingLicense))
            .WithMessage(localizer[AuthenticationResourcesKeys.IssuingCountryRequiredForPassport]);

        RuleFor(x => x.IssuingCountryCode)
            .Empty()
            .When(x => x.IdentityDocumentFile != null &&
                      (x.IdentityType == IdentityType.NationalId || x.IdentityType == IdentityType.Iqama))
            .WithMessage(localizer[AuthenticationResourcesKeys.IssuingCountryShouldNotBeProvided]);
    }
}
