using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommandValidator : AbstractValidator<VerifyOtpAndCreateAccountCommand>
{
    public VerifyOtpAndCreateAccountCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required")
            .Length(4).WithMessage("OTP code must be 4 digits");
    }
}
