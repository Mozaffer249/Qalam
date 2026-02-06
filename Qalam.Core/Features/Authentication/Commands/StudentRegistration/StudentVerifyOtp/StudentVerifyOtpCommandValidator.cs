using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentVerifyOtpCommandValidator : AbstractValidator<StudentVerifyOtpCommand>
{
    public StudentVerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.OtpCode).NotEmpty();
    }
}
