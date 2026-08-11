using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.UpdateMyProfilePicture;

public class UpdateMyProfilePictureCommandValidator
    : AbstractValidator<UpdateMyProfilePictureCommand>
{
    public UpdateMyProfilePictureCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Profile picture file is required.")
            .Must(f => f is { Length: > 0 })
            .WithMessage("Profile picture file is required.");
    }
}
