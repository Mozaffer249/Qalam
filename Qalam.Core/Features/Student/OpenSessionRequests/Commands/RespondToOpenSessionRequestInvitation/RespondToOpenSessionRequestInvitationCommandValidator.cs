using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.RespondToOpenSessionRequestInvitation;

public class RespondToOpenSessionRequestInvitationCommandValidator
    : AbstractValidator<RespondToOpenSessionRequestInvitationCommand>
{
    public RespondToOpenSessionRequestInvitationCommandValidator()
    {
        RuleFor(x => x.OpenSessionRequestId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();

        When(x => x.Data is not null, () =>
        {
            RuleFor(x => x.Data.StudentId).GreaterThan(0);
            RuleFor(x => x.Data.Decision)
                .Must(d => d == OpenSessionRequestInvitationStatus.Accepted
                       || d == OpenSessionRequestInvitationStatus.Rejected)
                .WithMessage("Decision يجب أن يكون Accepted أو Rejected فقط");
        });
    }
}
