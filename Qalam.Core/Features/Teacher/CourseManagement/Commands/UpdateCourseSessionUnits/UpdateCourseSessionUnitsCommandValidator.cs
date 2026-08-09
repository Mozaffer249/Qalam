using FluentValidation;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSessionUnits;

public class UpdateCourseSessionUnitsCommandValidator : AbstractValidator<UpdateCourseSessionUnitsCommand>
{
    private const int MaxUnitsPerSession = 20;

    public UpdateCourseSessionUnitsCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Units)
            .NotNull()
            .Must(us => us.Count <= MaxUnitsPerSession)
            .WithMessage($"Max {MaxUnitsPerSession} units/lessons per session.")
            .When(x => x.Data != null);

        RuleForEach(x => x.Data.Units)
            .ChildRules(u =>
            {
                u.RuleFor(x => x)
                    .Must(IsExactlyOneUnitSource)
                    .WithMessage("Exactly one of ContentUnitId, LessonId, or CustomUnitLabel must be set (not both, not neither).");
                u.RuleFor(x => x.CustomUnitLabel)
                    .MaximumLength(200)
                    .When(x => !string.IsNullOrWhiteSpace(x.CustomUnitLabel));
            })
            .When(x => x.Data != null && x.Data.Units != null);
    }

    private static bool IsExactlyOneUnitSource(CreateCourseSessionUnitDto unit)
    {
        var hasContentUnit = unit.ContentUnitId.HasValue;
        var hasLesson = unit.LessonId.HasValue;
        var hasCustom = !string.IsNullOrWhiteSpace(unit.CustomUnitLabel);
        return (hasContentUnit ? 1 : 0) + (hasLesson ? 1 : 0) + (hasCustom ? 1 : 0) == 1;
    }
}
