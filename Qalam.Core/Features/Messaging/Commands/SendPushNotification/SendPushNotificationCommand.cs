using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Messaging.Commands.SendPushNotification;

public class SendPushNotificationCommand : IRequest<Response<string>>
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public string? Icon { get; set; }
    public string? Sound { get; set; }
    public SendingStrategy Strategy { get; set; } = SendingStrategy.Fallback;
}
