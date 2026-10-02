using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Sessions.Commands.CancelStudentSession;
using Qalam.Core.Features.Student.Sessions.Commands.FileStudentSessionComplaint;
using Qalam.Core.Features.Student.Sessions.Commands.GetSessionLiveToken;
using Qalam.Core.Features.Student.Sessions.Commands.JoinSession;
using Qalam.Core.Features.Student.Sessions.Commands.SubmitSessionReview;
using Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionById;
using Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionComplaint;
using Qalam.Core.Features.Student.Sessions.Queries.ListStudentSessions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Live;
using Qalam.Data.DTOs.Policy;
using Qalam.Service.Models.Policy;
using Qalam.Data.DTOs.Student;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Student;

[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route(Router.StudentSessions)]
public class StudentSessionsController : AppControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<StudentSessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListStudentSessionsQuery(), cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentSessionDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
        => NewResult(await Mediator.Send(new GetStudentSessionByIdQuery { Id = id }));

    [HttpPost("{id:int}/Join")]
    [ProducesResponseType(typeof(StudentSessionJoinDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join(int id)
        => NewResult(await Mediator.Send(new JoinStudentSessionCommand { Id = id }));

    [HttpPost("{id:int}/LiveToken")]
    [ProducesResponseType(typeof(LiveSessionAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> LiveToken(int id)
        => NewResult(await Mediator.Send(new GetStudentSessionLiveTokenCommand { Id = id }));

    /// <summary>What cancelling (refund) or rescheduling this session would do, per the enrollment's policy.</summary>
    [HttpGet("{id:int}/Cancel/Preview")]
    [ProducesResponseType(typeof(PolicyPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewCancel(int id, [FromQuery] PolicyStudentChoice choice = PolicyStudentChoice.Refund)
        => NewResult(await Mediator.Send(new GetStudentSessionCancelPreviewQuery { ScheduleId = id, Choice = choice }));

    /// <summary>Cancel one session for a refund, or reschedule it to a new teacher slot.</summary>
    [HttpPost("{id:int}/Cancel")]
    [ProducesResponseType(typeof(PolicyOutcomeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelStudentSessionCommand body)
    {
        body.ScheduleId = id;
        return NewResult(await Mediator.Send(body));
    }

    [HttpPost("{id:int}/Review")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(int id, [FromBody] SubmitSessionReviewRequestDto body)
        => NewResult(await Mediator.Send(new SubmitStudentSessionReviewCommand
        {
            Id = id,
            Rating = body.Rating,
            Feedback = body.Feedback,
        }));

    [HttpPost("{id:int}/Complaints")]
    [ProducesResponseType(typeof(SessionComplaintDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> FileComplaint(
        int id,
        [FromForm] FileSessionComplaintRequest body,
        [FromForm] List<IFormFile>? attachments,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new FileStudentSessionComplaintCommand
        {
            ScheduleId = id,
            ReasonCode = body.ReasonCode,
            Description = body.Description,
            Attachments = attachments,
        }, cancellationToken));

    [HttpGet("Complaints/{complaintId:int}")]
    [ProducesResponseType(typeof(SessionComplaintDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplaint(int complaintId, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetStudentSessionComplaintQuery
        {
            ComplaintId = complaintId,
        }, cancellationToken));
}
