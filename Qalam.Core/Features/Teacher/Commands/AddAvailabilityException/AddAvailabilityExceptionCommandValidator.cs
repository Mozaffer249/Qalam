using FluentValidation;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.AddAvailabilityException;

public class AddAvailabilityExceptionCommandValidator : AbstractValidator<AddAvailabilityExceptionCommand>
{
    public AddAvailabilityExceptionCommandValidator()
    {
        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date cannot be in the past");

        RuleFor(x => x.TimeSlotId)
            .GreaterThan(0)
            .WithMessage("TimeSlotId must be greater than 0");

        RuleFor(x => x.ExceptionType)
            .IsInEnum()
            .WithMessage("ExceptionType must be valid (1 = Blocked, 2 = Extra)");

        RuleFor(x => x.Reason)
            .MaximumLength(250)
            .WithMessage("Reason cannot exceed 250 characters");
    }
}
