using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Enrollments.Commands.MarkEnrollmentConversationRead;
using Qalam.Core.Features.Teacher.Enrollments.Commands.PostEnrollmentConversationMessage;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetEnrollmentConversationMessages;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetOrCreateEnrollmentConversation;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Common;

/// <summary>
/// Enrollment-scoped teacher↔student chat. One conversation per enrollment.
/// Get-or-create is also exposed under Teacher/Enrollments/{id}/Conversation.
/// Message endpoints authorize either the owning teacher or the conversation's StudentUserId.
/// </summary>
[Authorize]
[ApiController]
public class EnrollmentConversationsController : AppControllerBase
{
    /// <summary>Find-or-create via shared path: Api/V1/EnrollmentConversations/by-enrollment/{enrollmentId}</summary>
    [HttpGet(Router.EnrollmentConversationByEnrollment)]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(EnrollmentConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateByEnrollment(int enrollmentId)
        => NewResult(await Mediator.Send(new GetOrCreateEnrollmentConversationQuery
        {
            EnrollmentId = enrollmentId
        }));

    /// <summary>Find-or-create via teacher enrollments path: Api/V1/Teacher/Enrollments/{id}/Conversation</summary>
    [HttpGet(Router.TeacherEnrollmentConversation)]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(EnrollmentConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateByTeacherEnrollment(int id)
        => NewResult(await Mediator.Send(new GetOrCreateEnrollmentConversationQuery
        {
            EnrollmentId = id
        }));

    /// <summary>
    /// Cursor-paginated messages. `cursor` = ISO-8601 SentAt of the boundary message;
    /// `direction` is "older" (default) or "newer".
    /// </summary>
    [HttpGet(Router.EnrollmentConversationMessages)]
    [ProducesResponseType(typeof(EnrollmentConversationMessagesPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] string? cursor, [FromQuery] int take = 50, [FromQuery] string direction = "older")
        => NewResult(await Mediator.Send(new GetEnrollmentConversationMessagesQuery
        {
            ConversationId = conversationId,
            Cursor = cursor,
            Take = take,
            Direction = direction
        }));

    /// <summary>Append a message. Sender is taken from the JWT.</summary>
    [HttpPost(Router.EnrollmentConversationMessages)]
    [ProducesResponseType(typeof(EnrollmentConversationMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PostMessage(int conversationId, [FromBody] PostEnrollmentConversationMessageDto dto)
        => NewResult(await Mediator.Send(new PostEnrollmentConversationMessageCommand
        {
            ConversationId = conversationId,
            Data = dto
        }));

    /// <summary>Mark messages as read. Idempotent.</summary>
    [HttpPost(Router.EnrollmentConversationMarkRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRead(int conversationId, [FromBody] MarkEnrollmentConversationReadDto? dto)
        => NewResult(await Mediator.Send(new MarkEnrollmentConversationReadCommand
        {
            ConversationId = conversationId,
            Data = dto ?? new MarkEnrollmentConversationReadDto()
        }));
}
