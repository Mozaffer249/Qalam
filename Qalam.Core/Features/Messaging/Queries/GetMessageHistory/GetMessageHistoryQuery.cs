using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Messaging;

namespace Qalam.Core.Features.Messaging.Queries.GetMessageHistory;

public class GetMessageHistoryQuery : IRequest<Response<List<MessageLog>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
