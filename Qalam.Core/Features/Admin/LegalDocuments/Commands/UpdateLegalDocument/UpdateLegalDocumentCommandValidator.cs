using FluentValidation;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocument;

public class UpdateLegalDocumentCommandValidator : AbstractValidator<UpdateLegalDocumentCommand>
{
    public UpdateLegalDocumentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.TitleEn).NotEmpty().MaximumLength(200);
    }
}
