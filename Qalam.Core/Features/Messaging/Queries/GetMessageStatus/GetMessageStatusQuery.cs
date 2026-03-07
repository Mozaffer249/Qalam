using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Messaging;

namespace Qalam.Core.Features.Messaging.Queries.GetMessageStatus;

public class GetMessageStatusQuery : IRequest<Response<MessageLog>>
{
    public string MessageId { get; set; } = string.Empty;
}
