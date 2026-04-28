using FluentValidation;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommandValidator : AbstractValidator<RequestCourseEnrollmentCommand>
{
    public RequestCourseEnrollmentCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.CourseId).GreaterThan(0).When(x => x.Data != null).WithMessage("Course is required.");
        RuleFor(x => x.Data.Notes).MaximumLength(400).When(x => x.Data != null);

        RuleFor(x => x.Data.StudentIds)
            .NotEmpty()
            .When(x => x.Data != null)
            .WithMessage("At least one student is required.");
        RuleForEach(x => x.Data.StudentIds)
            .GreaterThan(0)
            .When(x => x.Data != null);

        RuleForEach(x => x.Data.InvitedStudentIds)
            .GreaterThan(0)
            .When(x => x.Data != null);

        RuleFor(x => x.Data.SelectedAvailabilityIds)
            .NotEmpty()
            .When(x => x.Data != null)
            .WithMessage("At least one selected availability is required.");
        RuleForEach(x => x.Data.SelectedAvailabilityIds)
            .GreaterThan(0)
            .When(x => x.Data != null);

        RuleForEach(x => x.Data.ProposedSessions)
            .ChildRules(ps =>
            {
                ps.RuleFor(y => y.SessionNumber).GreaterThan(0);
                ps.RuleFor(y => y.DurationMinutes).GreaterThan(0);
                ps.RuleFor(y => y.Title).MaximumLength(150);
                ps.RuleFor(y => y.Notes).MaximumLength(500);
            })
            .When(x => x.Data != null);

        RuleFor(x => x.Data.PreferredStartDate)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.Data != null)
            .WithMessage("PreferredStartDate must be today or later.");

        RuleFor(x => x.Data.PreferredEndDate)
            .Must((cmd, end) => end >= cmd.Data.PreferredStartDate)
            .When(x => x.Data != null)
            .WithMessage("PreferredEndDate must be on or after PreferredStartDate.");
    }
}
