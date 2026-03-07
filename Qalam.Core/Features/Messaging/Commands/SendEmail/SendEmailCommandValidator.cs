using FluentValidation;

namespace Qalam.Core.Features.Messaging.Commands.SendEmail;

public class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required")
            .MaximumLength(200).WithMessage("Subject cannot exceed 200 characters");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required");
    }
}
