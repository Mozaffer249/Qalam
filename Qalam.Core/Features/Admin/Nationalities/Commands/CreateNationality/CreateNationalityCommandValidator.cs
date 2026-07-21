using FluentValidation;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.CreateNationality;

public class CreateNationalityCommandValidator : AbstractValidator<CreateNationalityCommand>
{
    public CreateNationalityCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Code must be a 2-letter ISO country code.");
        RuleFor(x => x.Data.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.FlagEmoji).MaximumLength(32).When(x => x.Data.FlagEmoji != null);
    }
}
