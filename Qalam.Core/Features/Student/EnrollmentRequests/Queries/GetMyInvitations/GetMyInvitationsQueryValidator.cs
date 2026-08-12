using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitations;

public class GetMyInvitationsQueryValidator : AbstractValidator<GetMyInvitationsQuery>
{
    public GetMyInvitationsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Scope).IsInEnum();
    }
}
