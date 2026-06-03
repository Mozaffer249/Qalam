using FluentValidation;

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
                    .Must(unit => unit.ContentUnitId.HasValue ^ unit.LessonId.HasValue)
                    .WithMessage("Exactly one of ContentUnitId or LessonId must be set (not both, not neither).");
            })
            .When(x => x.Data != null && x.Data.Units != null);
    }
}
