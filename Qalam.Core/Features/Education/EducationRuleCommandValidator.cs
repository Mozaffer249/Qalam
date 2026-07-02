using FluentValidation;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education;

public class EducationRuleDtoValidator : AbstractValidator<EducationRuleDto>
{
    public EducationRuleDtoValidator()
    {
        RuleFor(x => x.MinSessions)
            .GreaterThan(0).WithMessage("Min sessions must be greater than 0");

        RuleFor(x => x.MaxSessions)
            .GreaterThan(0).WithMessage("Max sessions must be greater than 0");

        RuleFor(x => x)
            .Must(x => x.MinSessions <= x.MaxSessions)
            .WithMessage("Min sessions cannot exceed max sessions");

        RuleFor(x => x.DefaultSessionDurationMinutes)
            .GreaterThan(0).WithMessage("Default session duration must be greater than 0");

        RuleFor(x => x)
            .Must(x => !x.MinGroupSize.HasValue || !x.MaxGroupSize.HasValue || x.MinGroupSize <= x.MaxGroupSize)
            .WithMessage("Min group size cannot exceed max group size");

        RuleFor(x => x.NotesAr)
            .MaximumLength(500).When(x => x.NotesAr != null);

        RuleFor(x => x.NotesEn)
            .MaximumLength(500).When(x => x.NotesEn != null);
    }
}
