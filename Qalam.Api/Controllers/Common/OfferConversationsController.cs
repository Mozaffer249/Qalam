using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.MarkConversationRead;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.PostConversationMessage;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetConversationMessages;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetOrCreateConversationByRequest;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Api.Controllers.Common;

/// <summary>
/// Conversations for Scenario 2 (Open Session Request). One conversation per (request, teacher)
/// pair — supports preliminary "طلب توضيح" chat before any offer is submitted, and persists
/// across withdraw + re-offer cycles. Both teacher and student/guardian access through the same
/// endpoints; the access guard derives the caller's role from the JWT.
/// </summary>
[Authorize]
[ApiController]
[Route(Router.OfferConversations)]
public class OfferConversationsController : AppControllerBase
{
    /// <summary>
    /// Find-or-create the (request, teacher) conversation and return its header. Either party
    /// can call this. The conversation exists independent of any offer.
    /// </summary>
    [HttpGet("by-request/{requestId:int}/teacher/{teacherId:int}")]
    [ProducesResponseType(typeof(OfferConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateByRequest(int requestId, int teacherId)
        => NewResult(await Mediator.Send(new GetOrCreateConversationByRequestQuery
        {
            RequestId = requestId,
            TeacherId = teacherId
        }));

    /// <summary>
    /// Cursor-paginated messages. `cursor` = ISO-8601 SentAt of the boundary message;
    /// `direction` is "older" (default) or "newer".
    /// </summary>
    [HttpGet("{conversationId:int}/messages")]
    [ProducesResponseType(typeof(ConversationMessagesPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] string? cursor, [FromQuery] int take = 50, [FromQuery] string direction = "older")
        => NewResult(await Mediator.Send(new GetConversationMessagesQuery
        {
            ConversationId = conversationId,
            Cursor = cursor,
            Take = take,
            Direction = direction
        }));

    /// <summary>Append a message. Sender is taken from the JWT.</summary>
    [HttpPost("{conversationId:int}/messages")]
    [ProducesResponseType(typeof(OfferConversationMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PostMessage(int conversationId, [FromBody] PostConversationMessageDto dto)
        => NewResult(await Mediator.Send(new PostConversationMessageCommand
        {
            ConversationId = conversationId,
            Data = dto
        }));

    /// <summary>Mark messages as read up to (and including) `upToMessageId`. Idempotent.</summary>
    [HttpPost("{conversationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRead(int conversationId, [FromBody] MarkConversationReadDto? dto)
        => NewResult(await Mediator.Send(new MarkConversationReadCommand
        {
            ConversationId = conversationId,
            Data = dto ?? new MarkConversationReadDto()
        }));
}
