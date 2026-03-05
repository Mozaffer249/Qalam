using FluentValidation;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.SearchStudentsForGroup;

public class SearchStudentsForGroupQueryValidator : AbstractValidator<SearchStudentsForGroupQuery>
{
    public SearchStudentsForGroupQueryValidator()
    {
        RuleFor(x => x.SearchTerm).NotEmpty().MinimumLength(2).MaximumLength(100)
            .WithMessage("Search term must be between 2 and 100 characters.");
        RuleFor(x => x.MaxResults).InclusiveBetween(1, 50);
    }
}
