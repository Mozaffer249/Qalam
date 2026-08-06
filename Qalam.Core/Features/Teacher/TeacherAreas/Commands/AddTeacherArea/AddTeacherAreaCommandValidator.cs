using FluentValidation;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Commands.AddTeacherArea;

public class AddTeacherAreaCommandValidator : AbstractValidator<AddTeacherAreaCommand>
{
    public AddTeacherAreaCommandValidator()
    {
        RuleFor(x => x.LocationId)
            .GreaterThan(0)
            .WithMessage("Location ID must be greater than 0");

        RuleFor(x => x.MaxDistanceKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxDistanceKm.HasValue)
            .WithMessage("Max distance must be 0 or greater");
    }
}
