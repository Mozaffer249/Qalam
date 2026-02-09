using FluentValidation;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Course.Commands.CreateCourse;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(200).When(x => x.Data != null);
        RuleFor(x => x.Data.Description).MaximumLength(2000).When(x => x.Data != null);
        RuleFor(x => x.Data.DomainId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.SubjectId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.TeachingModeId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.SessionTypeId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.Price).GreaterThanOrEqualTo(0).When(x => x.Data != null);
        RuleFor(x => x.Data.SessionsCount)
            .NotNull().GreaterThan(0)
            .When(x => x.Data != null && !x.Data.IsFlexible);
        RuleFor(x => x.Data.SessionDurationMinutes)
            .NotNull().GreaterThan(0)
            .When(x => x.Data != null && !x.Data.IsFlexible);
    }
}
