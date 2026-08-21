using FluentValidation;
using System.Text.RegularExpressions;

namespace Qalam.Core.Features.Admin.Pricing.Commands.CreateTeacherLevelTier;

public class CreateTeacherLevelTierCommandValidator : AbstractValidator<CreateTeacherLevelTierCommand>
{
    private static readonly Regex CodePattern = new("^[a-z0-9-]{2,50}$", RegexOptions.Compiled);

    public CreateTeacherLevelTierCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .Must(c => CodePattern.IsMatch(c.Trim().ToLowerInvariant()))
            .WithMessage("Code must be 2–50 lowercase letters, digits, or hyphens.");
        RuleFor(x => x.Data.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.TeacherSharePct).InclusiveBetween(0, 100);
    }
}
