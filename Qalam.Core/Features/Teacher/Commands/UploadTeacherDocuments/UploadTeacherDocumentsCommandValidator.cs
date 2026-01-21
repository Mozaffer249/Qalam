using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

public class UploadTeacherDocumentsCommandValidator : AbstractValidator<UploadTeacherDocumentsCommand>
{
    public UploadTeacherDocumentsCommandValidator()
    {
        // Identity document number validation
        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("Document number is required");

        // Identity document file validation
        RuleFor(x => x.IdentityDocumentFile)
            .NotNull().WithMessage("Identity document file is required");

        // Passport and DrivingLicense require country code
        RuleFor(x => x.IssuingCountryCode)
            .NotEmpty()
            .WithMessage("Issuing country is required for Passport or Driving License")
            .When(x => x.IdentityType == IdentityType.Passport ||
                      x.IdentityType == IdentityType.DrivingLicense);

        // NationalId and Iqama should not have country code
        RuleFor(x => x.IssuingCountryCode)
            .Empty()
            .WithMessage("Issuing country should not be provided for National ID or Iqama")
            .When(x => x.IdentityType == IdentityType.NationalId ||
                      x.IdentityType == IdentityType.Iqama);

        // Saudi Arabia validation
        RuleFor(x => x.IdentityType)
            .Must(type => type == IdentityType.NationalId || type == IdentityType.Iqama)
            .WithMessage("Teachers in Saudi Arabia must use National ID or Iqama")
            .When(x => x.IsInSaudiArabia);

        // Outside Saudi Arabia validation
        RuleFor(x => x.IdentityType)
            .Must(type => type == IdentityType.Passport || type == IdentityType.DrivingLicense)
            .WithMessage("Teachers outside Saudi Arabia must use Passport or Driving License")
            .When(x => !x.IsInSaudiArabia);

        // Certificates validation
        RuleFor(x => x.Certificates)
            .NotNull().WithMessage("Certificates are required")
            .Must(c => c != null && c.Count >= 1)
            .WithMessage("At least 1 certificate is required")
            .Must(c => c != null && c.Count <= 5)
            .WithMessage("Maximum 5 certificates allowed");

        RuleForEach(x => x.Certificates)
            .ChildRules(cert =>
            {
                cert.RuleFor(c => c.File)
                    .NotNull().WithMessage("Certificate file is required");
            });
    }
}
