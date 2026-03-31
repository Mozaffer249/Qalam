using FluentValidation;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.RejectEnrollmentRequest;

public class RejectEnrollmentRequestCommandValidator : AbstractValidator<RejectEnrollmentRequestCommand>
{
    public RejectEnrollmentRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0).WithMessage("RequestId is required.");
        RuleFor(x => x.Data.RejectionReason).MaximumLength(500).When(x => x.Data != null);
    }
}
