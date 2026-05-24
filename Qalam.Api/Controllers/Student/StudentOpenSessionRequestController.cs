using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.CancelOpenSessionRequest;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.CreateOpenSessionRequest;
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
}
