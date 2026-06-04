using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.PostConversationMessage;

public class PostConversationMessageCommand : IRequest<Response<OfferConversationMessageDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public int ConversationId { get; set; }
    public PostConversationMessageDto Data { get; set; } = default!;
}
