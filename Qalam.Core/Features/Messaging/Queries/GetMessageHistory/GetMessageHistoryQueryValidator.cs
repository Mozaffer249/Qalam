using FluentValidation;

namespace Qalam.Core.Features.Messaging.Queries.GetMessageHistory;

public class GetMessageHistoryQueryValidator : AbstractValidator<GetMessageHistoryQuery>
{
    public GetMessageHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200);
    }
}
