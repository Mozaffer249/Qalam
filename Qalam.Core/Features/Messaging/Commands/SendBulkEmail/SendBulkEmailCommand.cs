using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Features.Messaging.Commands.SendEmail;

namespace Qalam.Core.Features.Messaging.Commands.SendBulkEmail;

public class SendBulkEmailCommand : IRequest<Response<List<string>>>
{
    public List<SendEmailCommand> Requests { get; set; } = new();
}
