using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.DismissAvailableRequest;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.MarkAvailableRequestViewed;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.RejectAvailableRequest;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestAvailabilityMatch;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestById;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequests;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestsSummary;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Results;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher inbox for Scenario 2 (Open Session Requests) — list matched requests, view detail,
/// mark viewed, dismiss (target Skipped for stats), reject targeted requests, run availability-match.
/// Target status maps to inbox tabs only: Notified=new, Viewed=viewed, OfferSubmitted=offered, Skipped=dismissed.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherAvailableRequests)]
public class TeacherAvailableRequestsController : AppControllerBase
{
    /// <summary>Inbox list: matched requests the teacher can act on.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<TeacherAvailableRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] GetAvailableRequestsQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet("summary")]
    [ProducesResponseType(typeof(TeacherInboxSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary([FromQuery] GetAvailableRequestsSummaryQuery query)
        => NewResult(await Mediator.Send(query));

    /// <summary>Detail view — also flips the target row to Viewed on first call.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeacherAvailableRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
        => NewResult(await Mediator.Send(new GetAvailableRequestByIdQuery { RequestId = id }));

    /// <summary>Explicit mark-viewed endpoint (idempotent — only acts when status = Notified).</summary>
    [HttpPut("{id:int}/mark-viewed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkViewed(int id)
        => NewResult(await Mediator.Send(new MarkAvailableRequestViewedCommand { RequestId = id }));

    /// <summary>Hide a request from the inbox without rejecting it formally (target Skipped for stats).</summary>
    [HttpPost("{id:int}/dismiss")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Dismiss(int id)
        => NewResult(await Mediator.Send(new DismissAvailableRequestCommand { RequestId = id }));

    /// <summary>
    /// Teacher rejects a targeted request — sets OpenSessionRequest.Status = Rejected.
    /// Broadcast / non-targeted requests return NOT_TARGETED; use dismiss instead.
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(int id)
        => NewResult(await Mediator.Send(new RejectAvailableRequestCommand { RequestId = id }));

    /// <summary>
    /// Per-session availability + conflict map for the offer screen. For each session in the
    /// request, returns Available / Conflict / OutsideAvailability.
    /// </summary>
    [HttpGet("{id:int}/availability-match")]
    [ProducesResponseType(typeof(List<SessionAvailabilityMatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AvailabilityMatch(int id)
        => NewResult(await Mediator.Send(new GetAvailableRequestAvailabilityMatchQuery { RequestId = id }));
}
