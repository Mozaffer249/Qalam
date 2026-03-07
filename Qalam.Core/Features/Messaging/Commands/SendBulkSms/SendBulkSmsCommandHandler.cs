using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Commands.SendBulkSms;

public class SendBulkSmsCommandHandler : ResponseHandler, IRequestHandler<SendBulkSmsCommand, Response<List<string>>>
{
    private readonly ISmsService _smsService;
    private readonly IMessageTrackingService _messageTrackingService;
    private readonly ILogger<SendBulkSmsCommandHandler> _logger;

    public SendBulkSmsCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ISmsService smsService,
        IMessageTrackingService messageTrackingService,
        ILogger<SendBulkSmsCommandHandler> logger) : base(localizer)
    {
        _smsService = smsService;
        _messageTrackingService = messageTrackingService;
        _logger = logger;
    }

    public async Task<Response<List<string>>> Handle(SendBulkSmsCommand request, CancellationToken cancellationToken)
    {
        var messageIds = new List<string>();

        foreach (var smsRequest in request.Requests)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                await _messageTrackingService.LogMessageAsync(
                    messageId, MessageType.SMS, smsRequest.PhoneNumber, string.Empty, smsRequest.Content);
                await _smsService.SendSmsAsync(smsRequest.PhoneNumber, smsRequest.Content, smsRequest.Strategy);
                messageIds.Add(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to: {PhoneNumber}", smsRequest.PhoneNumber);
            }
        }

        return Success("Bulk SMS processed", messageIds);
    }
}
