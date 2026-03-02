using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Student;

[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route("Api/V1/Student")]
public class EnrollmentRequestMembershipController : AppControllerBase
{
    [HttpPost(Router.StudentEnrollmentRequestMemberResponse)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondToGroupInvite(
        int enrollmentRequestId,
        [FromBody] RespondToGroupEnrollmentInviteCommand command)
    {
        command.EnrollmentRequestId = enrollmentRequestId;
        return NewResult(await Mediator.Send(command));
    }
}
