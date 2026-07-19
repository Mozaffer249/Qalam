using FluentValidation;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CreateIndividualEnrollment;

public class CreateIndividualEnrollmentCommandValidator
    : AbstractValidator<CreateIndividualEnrollmentCommand>
{
    public CreateIndividualEnrollmentCommandValidator(ICourseRepository courseRepository)
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.CourseId).GreaterThan(0).When(x => x.Data != null);
        RuleFor(x => x.Data.Notes).MaximumLength(400).When(x => x.Data != null);

        RuleFor(x => x.Data.InvitedStudentIds)
            .Must(ids => ids == null || ids.Count == 0)
            .When(x => x.Data != null)
            .WithMessage("Invited students are not allowed for Individual enrollment.");

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
                if (course.IsFlexible) return false;

                var slots = data.SelectedSessionSlots ?? [];
                var fixedSessionCount = course.Sessions?.Count ?? 0;
                return slots.Count == fixedSessionCount;
            })
            .When(x => x.Data != null)
            .WithMessage(
                "Individual enrollment requires a Fixed course and selected slots matching course sessions.");

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
