using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.RespondToOpenSessionRequestInvitation;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Invited co-students respond (Accept / Reject) to an OpenSessionRequest invitation.
/// Mirrors <c>EnrollmentRequestMembershipController</c> for Scenario 1.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route("Api/V1/Student")]
public class OpenSessionRequestMembershipController : AppControllerBase
{
    /// <summary>
    /// Respond to an open-session-request invitation (Accepted or Rejected).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/OpenSessionRequests/{openSessionRequestId}/Members/Response
    ///
    /// Authorization:
    /// - Adult student: only the invited student themselves may respond.
    /// - Minor student: only the linked guardian may respond.
    ///
    /// On the last pending invitation flipping to a final state:
    /// - If at least one invitee accepted → request transitions PendingInvitations → Active
    ///   (matching engine triggers in a follow-up phase).
    /// - If all invitees rejected → request is auto-cancelled with reason.
    ///
    /// Sample request body:
    /// <code>
    /// {
    ///   "data": {
    ///     "studentId": 55,
    ///     "decision": "Accepted"
    ///   }
    /// }
    /// </code>
    /// </remarks>
    [HttpPost(Router.StudentOpenSessionRequestMemberResponse)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Respond(
        int openSessionRequestId,
        [FromBody] RespondToOpenSessionRequestInvitationCommand command)
    {
        command.OpenSessionRequestId = openSessionRequestId;
        return NewResult(await Mediator.Send(command));
    }
}
