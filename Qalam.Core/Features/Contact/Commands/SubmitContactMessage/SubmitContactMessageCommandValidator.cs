using FluentValidation;
using Qalam.Data.Entity.Common;

namespace Qalam.Core.Features.Contact.Commands.SubmitContactMessage;

public class SubmitContactMessageCommandValidator : AbstractValidator<SubmitContactMessageCommand>
{
    public SubmitContactMessageCommandValidator()
    {
        // Name/phone may be omitted when JWT is present; handler prefills from profile.
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone cannot exceed 30 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters")
            .EmailAddress().WithMessage("Invalid email address")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .Must(ContactReason.IsValid)
            .WithMessage("Invalid contact reason");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters");
    }
}
