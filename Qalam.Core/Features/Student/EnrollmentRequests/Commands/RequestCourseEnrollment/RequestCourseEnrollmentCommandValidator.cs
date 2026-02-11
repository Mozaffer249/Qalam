using FluentValidation;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommandValidator : AbstractValidator<RequestCourseEnrollmentCommand>
{
    public RequestCourseEnrollmentCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.CourseId).GreaterThan(0).When(x => x.Data != null).WithMessage("Course is required.");
        RuleFor(x => x.Data.TeachingModeId).GreaterThan(0).When(x => x.Data != null).WithMessage("Teaching mode is required.");
        RuleFor(x => x.Data.Notes).MaximumLength(400).When(x => x.Data != null);
    }
}
