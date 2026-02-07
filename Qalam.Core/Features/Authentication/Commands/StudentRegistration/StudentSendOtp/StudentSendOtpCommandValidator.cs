using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentSendOtpCommandValidator : AbstractValidator<StudentSendOtpCommand>
{
    public StudentSendOtpCommandValidator()
    {
        RuleFor(x => x.CountryCode)  // ← Validate CountryCode
            .NotEmpty()
            .WithMessage("Country code is required")
            .Matches(@"^\+[0-9]{1,4}$")
            .WithMessage("Country code must start with + and be 1-4 digits");

        RuleFor(x => x.PhoneNumber)  // ← Validate PhoneNumber
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^[0-9]{9,15}$")
            .WithMessage("Phone number must be 9-15 digits");
    }
}