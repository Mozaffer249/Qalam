using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Commands.SendBulkPush;

public class SendBulkPushCommandHandler : ResponseHandler, IRequestHandler<SendBulkPushCommand, Response<List<string>>>
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IMessageTrackingService _messageTrackingService;
    private readonly ILogger<SendBulkPushCommandHandler> _logger;

    public SendBulkPushCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IPushNotificationService pushNotificationService,
        IMessageTrackingService messageTrackingService,
        ILogger<SendBulkPushCommandHandler> logger) : base(localizer)
    {
        _pushNotificationService = pushNotificationService;
        _messageTrackingService = messageTrackingService;
        _logger = logger;
    }

    public async Task<Response<List<string>>> Handle(SendBulkPushCommand request, CancellationToken cancellationToken)
    {
        var messageIds = new List<string>();

        foreach (var pushRequest in request.Requests)
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                await _messageTrackingService.LogMessageAsync(
                    messageId, MessageType.PushNotification, pushRequest.DeviceToken, pushRequest.Title, pushRequest.Body);
                await _pushNotificationService.SendPushNotificationAsync(
                    pushRequest.DeviceToken, pushRequest.Title, pushRequest.Body, pushRequest.Strategy, pushRequest.Data);
                messageIds.Add(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to: {DeviceToken}", pushRequest.DeviceToken);
            }
        }

        return Success("Bulk push notifications processed", messageIds);
    }
}
