using FluentValidation;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourseSession;

public class UpdateCourseSessionCommandValidator : AbstractValidator<UpdateCourseSessionCommand>
{
    public UpdateCourseSessionCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Title).MaximumLength(150).When(x => x.Data != null);
        RuleFor(x => x.Data.Description).MaximumLength(1000).When(x => x.Data != null);
        RuleFor(x => x.Data.Notes).MaximumLength(500).When(x => x.Data != null);
    }
}
