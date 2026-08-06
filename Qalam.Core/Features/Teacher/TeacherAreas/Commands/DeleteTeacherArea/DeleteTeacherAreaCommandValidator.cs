using FluentValidation;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Commands.DeleteTeacherArea;

public class DeleteTeacherAreaCommandValidator : AbstractValidator<DeleteTeacherAreaCommand>
{
    public DeleteTeacherAreaCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Teacher area ID must be greater than 0");
    }
}
