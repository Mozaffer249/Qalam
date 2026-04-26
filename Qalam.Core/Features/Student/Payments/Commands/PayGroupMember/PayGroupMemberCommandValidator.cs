using FluentValidation;

namespace Qalam.Core.Features.Student.Payments.Commands.PayGroupMember;

public class PayGroupMemberCommandValidator : AbstractValidator<PayGroupMemberCommand>
{
    public PayGroupMemberCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.GroupEnrollmentId)
            .GreaterThan(0)
            .WithMessage("GroupEnrollmentId is required.")
            .When(x => x.Data != null);
        RuleFor(x => x.Data.StudentId)
            .GreaterThan(0)
            .WithMessage("StudentId is required.")
            .When(x => x.Data != null);
    }
}
