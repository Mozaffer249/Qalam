using FluentValidation;

namespace Qalam.Core.Features.Content.Commands.CreateContentUnit;

public class CreateContentUnitCommandValidator : AbstractValidator<CreateContentUnitCommand>
{
    public CreateContentUnitCommandValidator()
    {
        // Basic required fields
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required")
            .MaximumLength(200).WithMessage("Arabic name cannot exceed 200 characters");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required")
            .MaximumLength(200).WithMessage("English name cannot exceed 200 characters");

        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage("Subject ID is required and must be greater than 0");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Order index must be 0 or greater");

        // UnitTypeCode validation
        RuleFor(x => x.UnitTypeCode)
            .NotEmpty().WithMessage("Unit type code is required")
            .MaximumLength(50).WithMessage("Unit type code cannot exceed 50 characters")
            .Must(code => code == "SchoolUnit" || code == "QuranSurah" || code == "QuranPart" || code == "LanguageModule")
            .WithMessage("Unit type code must be one of: SchoolUnit, QuranSurah, QuranPart, LanguageModule");

        // Conditional validation for Quran Surah units
        RuleFor(x => x.QuranSurahId)
            .GreaterThan(0)
            .WithMessage("Quran Surah ID is required for QuranSurah unit type")
            .When(x => x.UnitTypeCode == "QuranSurah");

        RuleFor(x => x.QuranSurahId)
            .Null()
            .WithMessage("Quran Surah ID should not be set for non-QuranSurah unit types")
            .When(x => x.UnitTypeCode != "QuranSurah");

        // Conditional validation for Quran Part units
        RuleFor(x => x.QuranPartId)
            .GreaterThan(0)
            .WithMessage("Quran Part ID is required for QuranPart unit type")
            .When(x => x.UnitTypeCode == "QuranPart");

        RuleFor(x => x.QuranPartId)
            .Null()
            .WithMessage("Quran Part ID should not be set for non-QuranPart unit types")
            .When(x => x.UnitTypeCode != "QuranPart");

        // TermId validation - required for SchoolUnit, should be null for Quran units
        RuleFor(x => x.TermId)
            .NotNull()
            .WithMessage("Term ID is required for SchoolUnit type")
            .When(x => x.UnitTypeCode == "SchoolUnit");

        RuleFor(x => x.TermId)
            .GreaterThan(0)
            .WithMessage("Term ID must be greater than 0")
            .When(x => x.TermId.HasValue);

        // Business rule: Quran units should not have TermId
        RuleFor(x => x.TermId)
            .Null()
            .WithMessage("Term ID should not be set for Quran unit types (QuranSurah or QuranPart)")
            .When(x => x.UnitTypeCode == "QuranSurah" || x.UnitTypeCode == "QuranPart");

        // Business rule: Cannot have both QuranSurahId and QuranPartId
        RuleFor(x => x)
            .Must(x => !(x.QuranSurahId.HasValue && x.QuranSurahId > 0 && x.QuranPartId.HasValue && x.QuranPartId > 0))
            .WithMessage("Cannot specify both Quran Surah ID and Quran Part ID for the same unit");
    }
}
