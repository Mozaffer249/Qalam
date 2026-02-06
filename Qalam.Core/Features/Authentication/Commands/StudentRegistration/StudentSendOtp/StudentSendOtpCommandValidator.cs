using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentSendOtpCommandValidator : AbstractValidator<StudentSendOtpCommand>
{
    public StudentSendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required");
    }
}
