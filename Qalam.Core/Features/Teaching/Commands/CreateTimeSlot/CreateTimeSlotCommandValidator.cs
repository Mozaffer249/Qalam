using FluentValidation;

namespace Qalam.Core.Features.Teaching.Commands.CreateTimeSlot;

public class CreateTimeSlotCommandValidator : AbstractValidator<CreateTimeSlotCommand>
{
    public CreateTimeSlotCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.EndTime)
            .NotEqual(x => x.Data.StartTime)
            .WithMessage("End time must differ from start time");
        RuleFor(x => x.Data.DurationMinutes)
            .GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.LabelAr).MaximumLength(50);
        RuleFor(x => x.Data.LabelEn).MaximumLength(50);
    }
}
