using FluentValidation;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitationById;

public class GetMyInvitationByIdQueryValidator : AbstractValidator<GetMyInvitationByIdQuery>
{
    public GetMyInvitationByIdQueryValidator()
    {
        RuleFor(x => x.InvitationKey)
            .NotEmpty()
            .WithMessage("InvitationKey is required.");
    }
}
