using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.MarkConversationRead;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.PostConversationMessage;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetConversationMessages;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetOrCreateConversationByOffer;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetOrCreateConversationByRequest;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Api.Controllers.Common;

/// <summary>
/// Conversations for Scenario 2 (Open Session Request). Hybrid keying:
/// targeted requests → one thread per (request, teacher); broadcast → one thread per offer.
/// Both teacher and student/guardian access through the same endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route(Router.OfferConversations)]
public class OfferConversationsController : AppControllerBase
{
    /// <summary>
    /// Find-or-create the request-scoped conversation (targeted OSR only).
    /// Broadcast requests must use <see cref="GetOrCreateByOffer"/>.
    /// </summary>
    [HttpGet("by-request/{requestId:int}/teacher/{teacherId:int}")]
    [ProducesResponseType(typeof(OfferConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateByRequest(int requestId, int teacherId)
        => NewResult(await Mediator.Send(new GetOrCreateConversationByRequestQuery
        {
            RequestId = requestId,
            TeacherId = teacherId
        }));

    /// <summary>
    /// Find-or-create the conversation for an offer.
    /// Broadcast: offer-scoped thread. Targeted: resolves to the single request-scoped thread.
    /// </summary>
    [HttpGet("by-offer/{offerId:int}")]
    [ProducesResponseType(typeof(OfferConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateByOffer(int offerId)
        => NewResult(await Mediator.Send(new GetOrCreateConversationByOfferQuery
        {
            OfferId = offerId
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
