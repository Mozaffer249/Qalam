using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

public class UploadTeacherDocumentsCommandValidator : AbstractValidator<UploadTeacherDocumentsCommand>
{
    public UploadTeacherDocumentsCommandValidator()
    {
        RuleFor(x => x.Documents)
            .NotNull().WithMessage("Documents are required");

        RuleFor(x => x.Documents.IdentityDocument)
            .NotNull().WithMessage("Identity document is required")
            .DependentRules(() =>
            {
                RuleFor(x => x.Documents.IdentityDocument.DocumentNumber)
                    .NotEmpty().WithMessage("Document number is required");

                RuleFor(x => x.Documents.IdentityDocument.File)
                    .NotNull().WithMessage("Identity document file is required");

                // Passport requires country code
                RuleFor(x => x.Documents.IdentityDocument.IssuingCountryCode)
                    .NotEmpty()
                    .WithMessage("Issuing country is required for Passport")
                    .When(x => x.Documents.IdentityDocument.IdentityType == IdentityType.Passport);

                // NationalId and Iqama should not have country code
                RuleFor(x => x.Documents.IdentityDocument.IssuingCountryCode)
                    .Empty()
                    .WithMessage("Issuing country should not be provided for National ID or Iqama")
                    .When(x => x.Documents.IdentityDocument.IdentityType == IdentityType.NationalId ||
                              x.Documents.IdentityDocument.IdentityType == IdentityType.Iqama);
            });

        // Saudi Arabia validation
        RuleFor(x => x.Documents.IdentityDocument.IdentityType)
            .Must(type => type == IdentityType.NationalId || type == IdentityType.Iqama)
            .WithMessage("Teachers in Saudi Arabia must use National ID or Iqama")
            .When(x => x.Documents.IsInSaudiArabia);

        // Outside Saudi Arabia validation
        RuleFor(x => x.Documents.IdentityDocument.IdentityType)
            .Must(type => type == IdentityType.Passport)
            .WithMessage("Teachers outside Saudi Arabia must use Passport")
            .When(x => !x.Documents.IsInSaudiArabia);

        RuleFor(x => x.Documents.Certificates)
            .NotNull().WithMessage("Certificates are required")
            .Must(c => c != null && c.Count >= 1)
            .WithMessage("At least 1 certificate is required")
            .Must(c => c != null && c.Count <= 5)
            .WithMessage("Maximum 5 certificates allowed");

        RuleForEach(x => x.Documents.Certificates)
            .ChildRules(cert =>
            {
                cert.RuleFor(c => c.File)
                    .NotNull().WithMessage("Certificate file is required");
            });
    }
}
