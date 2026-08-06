using FluentValidation;

namespace Qalam.Core.Features.Teacher.Commands.DeleteTeacherSubject;

public class DeleteTeacherSubjectCommandValidator : AbstractValidator<DeleteTeacherSubjectCommand>
{
    public DeleteTeacherSubjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Teacher subject ID must be greater than 0");
    }
}
