using FluentValidation;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.CancelOpenSessionRequest;

public class CancelOpenSessionRequestCommandValidator : AbstractValidator<CancelOpenSessionRequestCommand>
{
    public CancelOpenSessionRequestCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data.Reason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Data.Reason));
    }
}
