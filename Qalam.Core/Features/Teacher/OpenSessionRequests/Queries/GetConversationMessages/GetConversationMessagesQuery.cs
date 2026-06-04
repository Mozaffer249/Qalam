using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetConversationMessages;

public class GetConversationMessagesQuery : IRequest<Response<ConversationMessagesPageDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ConversationId { get; set; }
    /// <summary>ISO-8601 SentAt of the last message returned in the prior page; null for first call.</summary>
    public string? Cursor { get; set; }
    public int Take { get; set; } = 50;
    /// <summary>"older" (default) or "newer".</summary>
    public string Direction { get; set; } = "older";
}
