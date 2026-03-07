using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Messaging.Commands.SendEmail;

public class SendEmailCommand : IRequest<Response<string>>
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public SendingStrategy Strategy { get; set; } = SendingStrategy.Fallback;
}
