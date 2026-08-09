using FluentValidation;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocument;

public class CreateLegalDocumentCommandValidator : AbstractValidator<CreateLegalDocumentCommand>
{
    public CreateLegalDocumentCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(50)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Code must be lowercase kebab-case.");
        RuleFor(x => x.Data.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.TitleEn).NotEmpty().MaximumLength(200);
    }
}
