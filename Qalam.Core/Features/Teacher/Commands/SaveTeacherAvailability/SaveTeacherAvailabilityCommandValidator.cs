using FluentValidation;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherAvailability;

public class SaveTeacherAvailabilityCommandValidator : AbstractValidator<SaveTeacherAvailabilityCommand>
{
    public SaveTeacherAvailabilityCommandValidator()
    {
        RuleFor(x => x.DaySchedules)
            .NotEmpty()
            .WithMessage("At least one day schedule is required");

        RuleForEach(x => x.DaySchedules).ChildRules(day =>
        {
            day.RuleFor(d => d.DayOfWeekId)
                .InclusiveBetween(1, 7)
                .WithMessage("DayOfWeekId must be between 1 and 7");

            day.RuleFor(d => d.TimeSlotIds)
                .NotEmpty()
                .WithMessage("At least one time slot is required per day");

            day.RuleForEach(d => d.TimeSlotIds)
                .GreaterThan(0)
                .WithMessage("TimeSlotId must be greater than 0");
        });
    }
}
