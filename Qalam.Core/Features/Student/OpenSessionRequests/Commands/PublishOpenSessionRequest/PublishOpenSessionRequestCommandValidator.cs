using FluentValidation;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.PublishOpenSessionRequest;

public class PublishOpenSessionRequestCommandValidator : AbstractValidator<PublishOpenSessionRequestCommand>
{
    public PublishOpenSessionRequestCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
