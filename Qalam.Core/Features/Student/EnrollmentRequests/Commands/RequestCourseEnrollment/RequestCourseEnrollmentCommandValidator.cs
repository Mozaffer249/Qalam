using FluentValidation;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommandValidator : AbstractValidator<RequestCourseEnrollmentCommand>
{
    public RequestCourseEnrollmentCommandValidator(ICourseRepository courseRepository)
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.CourseId).GreaterThan(0).When(x => x.Data != null).WithMessage("Course is required.");
        RuleFor(x => x.Data.Notes).MaximumLength(400).When(x => x.Data != null);

        RuleForEach(x => x.Data.StudentIds)
            .GreaterThan(0)
            .When(x => x.Data != null);

        RuleForEach(x => x.Data.InvitedStudentIds)
            .GreaterThan(0)
            .When(x => x.Data != null);

        RuleFor(x => x.Data.SelectedSessionSlots)
            .NotEmpty()
            .When(x => x.Data != null)
            .WithMessage("At least one session date selection is required.");
        RuleForEach(x => x.Data.SelectedSessionSlots)
            .ChildRules(s =>
            {
                s.RuleFor(x => x.SessionNumber).GreaterThan(0);
                s.RuleFor(x => x.TeacherAvailabilityId).GreaterThan(0);
            })
            .When(x => x.Data != null);

        RuleFor(x => x.Data)
            .MustAsync(async (data, cancellationToken) =>
            {
                if (data.CourseId <= 0) return true;
                var course = await courseRepository.GetByIdWithDetailsAsync(data.CourseId);
                if (course == null) return true;

                var slots = data.SelectedSessionSlots ?? [];
                var proposedCount = data.ProposedSessions?.Count ?? 0;
                var fixedSessionCount = course.Sessions?.Count ?? 0;

                if (!course.IsFlexible)
                    return slots.Count == fixedSessionCount;

                // Flexible: optional proposals — when provided, slot count must match; otherwise any non-empty selection is allowed.
                if (proposedCount > 0)
                    return slots.Count == proposedCount;

                return true;
            })
            .When(x => x.Data != null)
            .WithMessage(
                "Selected session slots must match the number of course sessions (non-flexible), or match ProposedSessions when proposals are sent (flexible).");

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
            .Must(d => !d.HasValue || d.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.Data != null)
            .WithMessage("PreferredStartDate must be today or later when provided.");

        RuleFor(x => x.Data)
            .Must(data =>
            {
                if (data == null || !data.PreferredEndDate.HasValue) return true;
                var start = data.PreferredStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                return data.PreferredEndDate.Value >= start;
            })
            .When(x => x.Data != null)
            .WithMessage("PreferredEndDate must be on or after PreferredStartDate (or today if start is omitted).");
    }
}
