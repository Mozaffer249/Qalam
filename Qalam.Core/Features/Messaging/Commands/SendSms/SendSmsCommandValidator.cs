using FluentValidation;

namespace Qalam.Core.Features.Messaging.Commands.SendSms;

public class SendSmsCommandValidator : AbstractValidator<SendSmsCommand>
{
    public SendSmsCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(1600).WithMessage("Content cannot exceed 1600 characters");
    }
}
