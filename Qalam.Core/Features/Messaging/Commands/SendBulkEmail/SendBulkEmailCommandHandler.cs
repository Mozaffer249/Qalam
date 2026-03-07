using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Commands.SendBulkEmail;

public class SendBulkEmailCommandHandler : ResponseHandler, IRequestHandler<SendBulkEmailCommand, Response<List<string>>>
{
    private readonly IEmailService _emailService;
    private readonly IMessageTrackingService _messageTrackingService;
    private readonly ILogger<SendBulkEmailCommandHandler> _logger;

    public SendBulkEmailCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEmailService emailService,
        IMessageTrackingService messageTrackingService,
        ILogger<SendBulkEmailCommandHandler> logger) : base(localizer)
    {
        _emailService = emailService;
        _messageTrackingService = messageTrackingService;
        _logger = logger;
    }

    public async Task<Response<List<string>>> Handle(SendBulkEmailCommand request, CancellationToken cancellationToken)
    {
        var messageIds = new List<string>();

        foreach (var emailRequest in request.Requests)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                await _messageTrackingService.LogMessageAsync(
                    messageId, MessageType.Email, emailRequest.To, emailRequest.Subject, emailRequest.Body);
                await _emailService.SendEmailAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body, emailRequest.Strategy);
                messageIds.Add(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to: {To}", emailRequest.To);
            }
        }

        return Success("Bulk email processed", messageIds);
    }
}
