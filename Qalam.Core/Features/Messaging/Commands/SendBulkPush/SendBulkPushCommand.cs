using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Features.Messaging.Commands.SendPushNotification;

namespace Qalam.Core.Features.Messaging.Commands.SendBulkPush;

public class SendBulkPushCommand : IRequest<Response<List<string>>>
{
    public List<SendPushNotificationCommand> Requests { get; set; } = new();
}
