using FluentValidation;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentSection;

public class UpdateLegalDocumentSectionCommandValidator : AbstractValidator<UpdateLegalDocumentSectionCommand>
{
    public UpdateLegalDocumentSectionCommandValidator()
    {
        RuleFor(x => x.SectionId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.TitleAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Data.TitleEn).NotEmpty().MaximumLength(300);
        When(x => !string.IsNullOrWhiteSpace(x.Data.AnchorKey), () =>
        {
            RuleFor(x => x.Data.AnchorKey!).MaximumLength(100)
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        });
    }
}
