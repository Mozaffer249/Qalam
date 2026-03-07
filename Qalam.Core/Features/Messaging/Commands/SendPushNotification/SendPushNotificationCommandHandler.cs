using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Commands.SendPushNotification;

public class SendPushNotificationCommandHandler : ResponseHandler, IRequestHandler<SendPushNotificationCommand, Response<string>>
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IMessageTrackingService _messageTrackingService;

    public SendPushNotificationCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IPushNotificationService pushNotificationService,
        IMessageTrackingService messageTrackingService) : base(localizer)
    {
        _pushNotificationService = pushNotificationService;
        _messageTrackingService = messageTrackingService;
    }

    public async Task<Response<string>> Handle(SendPushNotificationCommand request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString();

        await _messageTrackingService.LogMessageAsync(
            messageId, MessageType.PushNotification, request.DeviceToken, request.Title, request.Body);

        await _pushNotificationService.SendPushNotificationAsync(
            request.DeviceToken, request.Title, request.Body, request.Strategy, request.Data);

        return Success<string>("Push notification processed successfully", messageId);
    }
}
