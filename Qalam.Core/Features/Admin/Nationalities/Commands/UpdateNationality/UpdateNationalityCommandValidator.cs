using FluentValidation;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.UpdateNationality;

public class UpdateNationalityCommandValidator : AbstractValidator<UpdateNationalityCommand>
{
    public UpdateNationalityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.FlagEmoji).MaximumLength(32).When(x => x.Data.FlagEmoji != null);
    }
}
