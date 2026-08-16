using FluentValidation;

namespace Qalam.Core.Features.Education.Commands.UpsertWritableFilterValue;

public class UpsertWritableFilterValueCommandValidator : AbstractValidator<UpsertWritableFilterValueCommand>
{
    public UpsertWritableFilterValueCommandValidator()
    {
        RuleFor(x => x.DomainId).GreaterThan(0);
        RuleFor(x => x.SlotCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200).When(x => x.NameEn != null);
    }
}
