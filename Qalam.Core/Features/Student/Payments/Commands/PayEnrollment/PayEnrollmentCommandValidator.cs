using FluentValidation;

namespace Qalam.Core.Features.Student.Payments.Commands.PayEnrollment;

public class PayEnrollmentCommandValidator : AbstractValidator<PayEnrollmentCommand>
{
    public PayEnrollmentCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.EnrollmentId)
            .GreaterThan(0)
            .WithMessage("EnrollmentId is required.")
            .When(x => x.Data != null);
    }
}
