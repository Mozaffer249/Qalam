using FluentValidation;

namespace Qalam.Core.Features.Student.Commands.UpdateChildProfilePicture;

public class UpdateChildProfilePictureCommandValidator
    : AbstractValidator<UpdateChildProfilePictureCommand>
{
    public UpdateChildProfilePictureCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a positive number.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("Profile picture file is required.")
            .Must(f => f is { Length: > 0 })
            .WithMessage("Profile picture file is required.");
    }
}
