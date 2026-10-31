using FluentValidation;

namespace Qalam.Core.Features.Student.Queries.GetStudentAllowedIdentityTypes;

public class GetStudentAllowedIdentityTypesQueryValidator : AbstractValidator<GetStudentAllowedIdentityTypesQuery>
{
    public GetStudentAllowedIdentityTypesQueryValidator()
    {
        RuleFor(x => x.NationalityId)
            .GreaterThan(0).WithMessage("NationalityId is required.");
    }
}
