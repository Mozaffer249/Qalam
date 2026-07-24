using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Sessions.Commands.JoinSession;
using Qalam.Core.Features.Student.Sessions.Commands.SubmitSessionReview;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Student;

[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route(Router.StudentSessions)]
public class StudentSessionsController : AppControllerBase
{
    [HttpPost("{id:int}/Join")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join(int id)
        => NewResult(await Mediator.Send(new JoinStudentSessionCommand { Id = id }));

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
