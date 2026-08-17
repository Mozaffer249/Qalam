using FluentValidation;

namespace Qalam.Core.Features.Education.Commands.UpsertWritableFilterValue;

public class UpsertWritableFilterValueCommandValidator : AbstractValidator<UpsertWritableFilterValueCommand>
{
    public UpsertWritableFilterValueCommandValidator()
    {
        RuleFor(x => x.DomainId).GreaterThan(0);
        RuleFor(x => x.SlotCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || !string.IsNullOrWhiteSpace(x.NameAr))
            .WithMessage("Text or NameAr is required");
        RuleFor(x => x.Text).MaximumLength(200).When(x => x.Text != null);
        RuleFor(x => x.NameAr).MaximumLength(200).When(x => x.NameAr != null);
        RuleFor(x => x.NameEn).MaximumLength(200).When(x => x.NameEn != null);
        RuleFor(x => x.Code).MaximumLength(80).When(x => x.Code != null);
        RuleFor(x => x.SubjectCodeContains).MaximumLength(40).When(x => x.SubjectCodeContains != null);
    }
}
