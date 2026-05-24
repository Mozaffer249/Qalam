using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.CancelOpenSessionRequest;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.CreateOpenSessionRequest;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.DeleteOpenSessionRequestAttachment;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.UploadOpenSessionRequestAttachment;
using Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetMyOpenSessionRequests;
using Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetOpenSessionRequestById;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Open Session Request — student (or guardian on behalf of a minor) posts a request
/// for sessions and receives offers from qualified teachers.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route("Api/V1/Student")]
public class StudentOpenSessionRequestController : AppControllerBase
{
    /// <summary>
    /// Create + publish a new open session request. Triggers the matching engine when no
    /// invitations are pending; otherwise the request waits in PendingInvitations until all
    /// invited students respond.
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/OpenSessionRequests
    ///
    /// Authorization:
    /// - Adult student: `data.studentId` must be the caller's linked Student.Id.
    /// - Guardian on behalf of minor: `data.studentId` is the child Student.Id; guardian must
    ///   be the linked Guardian. The server stores `createdByGuardianId` for audit.
    ///
    /// Status transitions on create:
    /// - No invitations → `Active` (matching kicks off).
    /// - With invitations → `PendingInvitations` (matching waits until all invitees respond).
    ///
    /// `expiresAt` defaults to PublishedAt + 7 days if omitted.
    /// </remarks>
    [HttpPost(Router.StudentOpenSessionRequests)]
    [ProducesResponseType(typeof(OpenSessionRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateOpenSessionRequestCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get full detail of one open session request. Visible to the request owner (or their
    /// guardian) and to any student who was invited as a co-learner.
    /// </summary>
    [HttpGet(Router.StudentOpenSessionRequestById)]
    [ProducesResponseType(typeof(OpenSessionRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return NewResult(await Mediator.Send(new GetOpenSessionRequestByIdQuery { Id = id }));
    }

    /// <summary>
    /// List the caller's open session requests (created by self OR by self as a guardian on
    /// behalf of a child). Optional status filter.
    /// </summary>
    [HttpGet(Router.StudentOpenSessionRequestsMy)]
    [ProducesResponseType(typeof(List<OpenSessionRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy([FromQuery] OpenSessionRequestStatus? status)
    {
        return NewResult(await Mediator.Send(new GetMyOpenSessionRequestsQuery { Status = status }));
    }

    /// <summary>
    /// Cancel a request. Allowed while status is in
    /// { Draft, PendingInvitations, Active, ReceivingOffers }. Any Pending offers are bulk-
    /// withdrawn so teachers stop seeing the request as actionable.
    /// </summary>
    [HttpPost(Router.StudentOpenSessionRequestCancel)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelOpenSessionRequestCommand command)
    {
        command.Id = id;
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Upload a single attachment file to an open session request (PDF, DOC, DOCX, PNG, JPG; max 25 MB; max 10 per request).
    /// The file is queued to OSS via RabbitMQ; the response carries the new attachment row with a placeholder
    /// <c>storageKey</c>. The <c>publicUrl</c> field is populated by the consumer shortly after upload completes.
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/OpenSessionRequests/{id}/Attachments
    /// Content-Type: multipart/form-data
    /// Body: <c>file</c> (single IFormFile)
    /// </remarks>
    [HttpPost(Router.StudentOpenSessionRequestAttachments)]
    [RequestSizeLimit(26 * 1024 * 1024)] // 25 MB + headroom
    [ProducesResponseType(typeof(OpenSessionRequestAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAttachment(int id, [FromForm] IFormFile file)
    {
        var command = new UploadOpenSessionRequestAttachmentCommand
        {
            OpenSessionRequestId = id,
            File = file,
        };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Remove an attachment from an open session request. Allowed while the request is still editable
    /// (Draft / PendingInvitations / Active / ReceivingOffers).
    /// </summary>
    [HttpDelete(Router.StudentOpenSessionRequestAttachmentById)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
    {
        var command = new DeleteOpenSessionRequestAttachmentCommand
        {
            OpenSessionRequestId = id,
            AttachmentId = attachmentId,
        };
        return NewResult(await Mediator.Send(command));
    }
}
