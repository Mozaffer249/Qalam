using FluentValidation;

namespace Qalam.Core.Features.Messaging.Commands.SendPushNotification;

public class SendPushNotificationCommandValidator : AbstractValidator<SendPushNotificationCommand>
{
    public SendPushNotificationCommandValidator()
    {
        RuleFor(x => x.DeviceToken)
            .NotEmpty().WithMessage("Device token is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required")
            .MaximumLength(500).WithMessage("Body cannot exceed 500 characters");
    }
}
