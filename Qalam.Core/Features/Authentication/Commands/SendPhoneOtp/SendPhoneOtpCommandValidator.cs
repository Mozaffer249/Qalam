using FluentValidation;

namespace Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;

public class SendPhoneOtpCommandValidator : AbstractValidator<SendPhoneOtpCommand>
{
    public SendPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^[0-9]{9,15}$").WithMessage("Phone number must be 9-15 digits");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required")
            .Matches(@"^\+[0-9]{1,4}$").WithMessage("Country code must start with + and be 1-4 digits");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!)
                .EmailAddress().WithMessage("Email format is invalid");
        });
    }
}
