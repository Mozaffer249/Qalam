using FluentValidation;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentSection;

public class CreateLegalDocumentSectionCommandValidator : AbstractValidator<CreateLegalDocumentSectionCommand>
{
    public CreateLegalDocumentSectionCommandValidator()
    {
        RuleFor(x => x.VersionId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.AnchorKey).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(x => x.Data.TitleAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Data.TitleEn).NotEmpty().MaximumLength(300);
    }
}
