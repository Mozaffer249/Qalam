using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Messaging.Commands.SendSms;

public class SendSmsCommand : IRequest<Response<string>>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public SendingStrategy Strategy { get; set; } = SendingStrategy.Fallback;
}
