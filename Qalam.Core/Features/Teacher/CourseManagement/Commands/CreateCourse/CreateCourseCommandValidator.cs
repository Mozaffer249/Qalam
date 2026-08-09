using FluentValidation;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.CreateCourse;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(200).When(x => x.Data != null);
        RuleFor(x => x.Data.Description).MaximumLength(2000).When(x => x.Data != null);
        RuleFor(x => x.Data.TeacherSubjectId)
            .GreaterThan(0)
            .When(x => x.Data != null)
            .WithMessage("Please select a subject from your profile");
        RuleFor(x => x.Data.TeachingModeId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.SessionTypeId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.Price).GreaterThanOrEqualTo(0).When(x => x.Data != null);
        RuleFor(x => x.Data.SessionDurationMinutes)
            .GreaterThan(0)
            .When(x => x.Data != null && !x.Data.IsFlexible && x.Data.SessionDurationMinutes.HasValue);
        RuleFor(x => x.Data.SessionDurationMinutes)
            .Null()
            .When(x => x.Data != null && x.Data.IsFlexible)
            .WithMessage("SessionDurationMinutes must be null when course is flexible.");
        RuleFor(x => x.Data.MaxStudents)
            .GreaterThanOrEqualTo(2)
            .When(x => x.Data != null && x.Data.MaxStudents.HasValue);

        RuleFor(x => x.Data.Sessions)
            .NotNull()
            .Must(list => list != null && list.Count > 0)
            .When(x => x.Data != null && !x.Data.IsFlexible)
            .WithMessage("Sessions are required when course is not flexible.");

        RuleFor(x => x.Data.Sessions)
            .Must(list => list == null || list.Count == 0)
            .When(x => x.Data != null && x.Data.IsFlexible)
            .WithMessage("Sessions must be empty when course is flexible.");

        When(x => x.Data != null && x.Data.Sessions != null && x.Data.Sessions.Count > 0, () =>
        {
            RuleForEach(x => x.Data.Sessions).ChildRules(s =>
            {
                s.RuleFor(i => i.DurationMinutes).GreaterThan(0);
                s.RuleFor(i => i.Title).MaximumLength(150);
                s.RuleFor(i => i.Notes).MaximumLength(500);

                s.RuleFor(i => i.Units)
                    .Must(us => (us?.Count ?? 0) <= MaxUnitsPerSession)
                    .WithMessage($"Max {MaxUnitsPerSession} units/lessons per session.");

                s.RuleForEach(i => i.Units!)
                    .ChildRules(u =>
                    {
                        u.RuleFor(x => x)
                            .Must(IsExactlyOneUnitSource)
                            .WithMessage("Exactly one of ContentUnitId, LessonId, or CustomUnitLabel must be set (not both, not neither).");
                        u.RuleFor(x => x.CustomUnitLabel)
                            .MaximumLength(200)
                            .When(x => !string.IsNullOrWhiteSpace(x.CustomUnitLabel));
                    })
                    .When(i => i.Units != null);
            });
        });
    }

    private const int MaxUnitsPerSession = 20;

    private static bool IsExactlyOneUnitSource(CreateCourseSessionUnitDto unit)
    {
        var hasContentUnit = unit.ContentUnitId.HasValue;
        var hasLesson = unit.LessonId.HasValue;
        var hasCustom = !string.IsNullOrWhiteSpace(unit.CustomUnitLabel);
        return (hasContentUnit ? 1 : 0) + (hasLesson ? 1 : 0) + (hasCustom ? 1 : 0) == 1;
    }
}
