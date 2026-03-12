using FluentValidation;

namespace Qalam.Core.Features.Student.Queries.SearchStudents;

public class SearchStudentsQueryValidator : AbstractValidator<SearchStudentsQuery>
{
    public SearchStudentsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required.")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50);
    }
}
