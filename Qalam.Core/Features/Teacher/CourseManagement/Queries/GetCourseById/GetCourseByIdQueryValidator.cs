using FluentValidation;

namespace Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCourseById;

public class GetCourseByIdQueryValidator : AbstractValidator<GetCourseByIdQuery>
{
    public GetCourseByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
