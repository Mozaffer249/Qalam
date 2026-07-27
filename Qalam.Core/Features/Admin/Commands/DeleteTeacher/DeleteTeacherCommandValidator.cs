using FluentValidation;

namespace Qalam.Core.Features.Admin.Commands.DeleteTeacher;

public class DeleteTeacherCommandValidator : AbstractValidator<DeleteTeacherCommand>
{
    public DeleteTeacherCommandValidator()
    {
        RuleFor(x => x.TeacherId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
    }
}
