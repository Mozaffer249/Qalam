using FluentValidation;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.ApproveEnrollmentRequest;

public class ApproveEnrollmentRequestCommandValidator : AbstractValidator<ApproveEnrollmentRequestCommand>
{
    public ApproveEnrollmentRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0).WithMessage("RequestId is required.");
    }
}
