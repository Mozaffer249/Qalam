using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Commands.SendEmail;

public class SendEmailCommandHandler : ResponseHandler, IRequestHandler<SendEmailCommand, Response<string>>
{
    private readonly IEmailService _emailService;
    private readonly IMessageTrackingService _messageTrackingService;

    public SendEmailCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEmailService emailService,
        IMessageTrackingService messageTrackingService) : base(localizer)
    {
        _emailService = emailService;
        _messageTrackingService = messageTrackingService;
    }

    public async Task<Response<string>> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString();

        await _messageTrackingService.LogMessageAsync(
            messageId, MessageType.Email, request.To, request.Subject, request.Body);

        await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, request.Strategy);

        return Success<string>("Email processed successfully", messageId);
    }
}
