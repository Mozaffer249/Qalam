using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

public class UploadTeacherDocumentsCommandValidator : AbstractValidator<UploadTeacherDocumentsCommand>
{
    public UploadTeacherDocumentsCommandValidator(IStringLocalizer<AuthenticationResources> localizer)
    {
        // Identity document number validation
        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.DocumentNumberRequired]);

        // Identity document file validation
        RuleFor(x => x.IdentityDocumentFile)
            .NotNull()
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.IdentityDocumentFileRequired]);

        // Passport and DrivingLicense require country code
        RuleFor(x => x.IssuingCountryCode)
            .NotEmpty()
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.IssuingCountryRequiredForPassport])
            .When(x => x.IdentityType == IdentityType.Passport ||
                      x.IdentityType == IdentityType.DrivingLicense);

        // NationalId and Iqama should not have country code
        RuleFor(x => x.IssuingCountryCode)
            .Empty()
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.IssuingCountryShouldNotBeProvided])
            .When(x => x.IdentityType == IdentityType.NationalId ||
                      x.IdentityType == IdentityType.Iqama);

        // Saudi Arabia validation
        RuleFor(x => x.IdentityType)
            .Must(type => type == IdentityType.NationalId || type == IdentityType.Iqama)
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.TeachersSaudiMustUseNationalIdOrIqama])
            .When(x => x.IsInSaudiArabia);

        // Outside Saudi Arabia validation
        RuleFor(x => x.IdentityType)
            .Must(type => type == IdentityType.Passport || type == IdentityType.DrivingLicense)
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.TeachersOutsideSaudiMustUsePassport])
            .When(x => !x.IsInSaudiArabia);

        // Certificates validation
        RuleFor(x => x.Certificates)
            .NotNull()
            .WithName("")
            .WithMessage(localizer[AuthenticationResourcesKeys.CertificatesRequired])
            .Must(c => c != null && c.Count >= 1)
            .WithMessage(localizer[AuthenticationResourcesKeys.AtLeastOneCertificateRequired])
            .Must(c => c != null && c.Count <= 5)
            .WithMessage(localizer[AuthenticationResourcesKeys.MaximumFiveCertificatesAllowed]);

        RuleForEach(x => x.Certificates)
            .ChildRules(cert =>
            {
                cert.RuleFor(c => c.File)
                    .NotNull()
                    .WithName("")
                    .WithMessage(localizer[AuthenticationResourcesKeys.CertificateFileRequired]);
            });
    }
}
