using FluentValidation;

namespace Qalam.Core.Features.Education.Commands.UpdateWritableFilterValue;

public class UpdateWritableFilterValueCommandValidator : AbstractValidator<UpdateWritableFilterValueCommand>
{
    public UpdateWritableFilterValueCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(80).When(x => x.Code != null);
        RuleFor(x => x.SubjectCodeContains).MaximumLength(40).When(x => x.SubjectCodeContains != null);
    }
}
