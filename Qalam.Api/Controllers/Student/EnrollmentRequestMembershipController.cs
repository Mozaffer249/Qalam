using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.EnrollmentRequests.Commands.CancelGroupEnrollmentInvite;
using Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Invited students respond (Accept/Reject) to a group-enrollment invitation;
/// owners cancel pending invites.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route("Api/V1/Student")]
public class EnrollmentRequestMembershipController : AppControllerBase
{
    /// <summary>
    /// Respond to a group-enrollment invitation (Confirm or Reject).
    /// </summary>
    [HttpPost(Router.StudentEnrollmentRequestMemberResponse)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondToGroupInvite(
        int enrollmentRequestId,
        [FromBody] RespondToGroupEnrollmentInviteCommand command)
    {
        command.EnrollmentRequestId = enrollmentRequestId;
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Owner cancels a pending Invited member before enrollment is created.
    /// </summary>
    [HttpPost(Router.StudentEnrollmentRequestCancelInvite)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelGroupInvite(int enrollmentRequestId, int studentId)
    {
        var command = new CancelGroupEnrollmentInviteCommand
        {
            EnrollmentRequestId = enrollmentRequestId,
            StudentId = studentId
        };
        return NewResult(await Mediator.Send(command));
    }
}
