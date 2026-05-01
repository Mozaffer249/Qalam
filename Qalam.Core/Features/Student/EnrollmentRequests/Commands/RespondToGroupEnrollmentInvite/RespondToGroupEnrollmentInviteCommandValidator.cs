using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;

public class RespondToGroupEnrollmentInviteCommandValidator : AbstractValidator<RespondToGroupEnrollmentInviteCommand>
{
    public RespondToGroupEnrollmentInviteCommandValidator()
    {
        RuleFor(x => x.EnrollmentRequestId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.StudentId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.Decision)
            .Must(d => d == GroupMemberConfirmationStatus.Confirmed
                || d == GroupMemberConfirmationStatus.Rejected
                || d == GroupMemberConfirmationStatus.Cancelled)
            .When(x => x.Data != null)
            .WithMessage("Decision must be Confirmed, Rejected, or Cancelled.");
    }
}
