using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Features.Messaging.Commands.SendSms;

namespace Qalam.Core.Features.Messaging.Commands.SendBulkSms;

public class SendBulkSmsCommand : IRequest<Response<List<string>>>
{
    public List<SendSmsCommand> Requests { get; set; } = new();
}
