using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Sessions.Commands.GetSessionLiveToken;
using Qalam.Core.Features.Student.Sessions.Commands.JoinSession;
using Qalam.Core.Features.Student.Sessions.Commands.SubmitSessionReview;
using Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionById;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Live;
using Qalam.Data.DTOs.Student;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Student;

[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route(Router.StudentSessions)]
public class StudentSessionsController : AppControllerBase
{
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

    [HttpPost("{id:int}/Review")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(int id, [FromBody] SubmitSessionReviewRequestDto body)
        => NewResult(await Mediator.Send(new SubmitStudentSessionReviewCommand
        {
            Id = id,
            Rating = body.Rating,
            Feedback = body.Feedback,
        }));
}
