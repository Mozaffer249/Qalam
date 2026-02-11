using FluentValidation;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.DeleteCourse;

public class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
