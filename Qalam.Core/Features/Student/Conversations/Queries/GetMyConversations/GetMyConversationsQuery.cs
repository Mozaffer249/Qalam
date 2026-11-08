using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Conversations;

namespace Qalam.Core.Features.Student.Conversations.Queries.GetMyConversations;

public class GetMyConversationsQuery : IRequest<Response<List<MyConversationListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
