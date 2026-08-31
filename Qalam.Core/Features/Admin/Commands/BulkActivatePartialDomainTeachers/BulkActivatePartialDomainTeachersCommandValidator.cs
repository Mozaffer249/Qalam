using FluentValidation;

namespace Qalam.Core.Features.Admin.Commands.BulkActivatePartialDomainTeachers;

public class BulkActivatePartialDomainTeachersCommandValidator : AbstractValidator<BulkActivatePartialDomainTeachersCommand>
{
    public BulkActivatePartialDomainTeachersCommandValidator()
    {
        RuleFor(x => x.TeacherIds)
            .NotEmpty()
            .WithMessage("At least one teacher ID is required");

        RuleForEach(x => x.TeacherIds)
            .GreaterThan(0)
            .WithMessage("Teacher ID must be greater than 0");
    }
}
