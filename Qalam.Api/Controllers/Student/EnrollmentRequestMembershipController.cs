using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Invited students respond (Accept/Reject) to a group-enrollment invitation.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route("Api/V1/Student")]
public class EnrollmentRequestMembershipController : AppControllerBase
{
    /// <summary>
    /// Respond to a group-enrollment invitation (Confirm or Reject).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response
    ///
    /// Authorization:
    /// - Adult student: only the student themselves can respond.
    /// - Minor student: only the linked guardian can respond.
    ///
    /// When responses are accepted (by request status + course type):
    /// - Flexible course (`Pending` request) — invitees may respond while the request is still
    ///   awaiting teacher approval.
    /// - Fixed course (`Approved` request) — the request auto-approved on submit; invitees can
    ///   still respond to finalize the group. When the **last** invitee responds and at least
    ///   one member is Confirmed, the unified <c>Enrollment</c> + <c>EnrollmentParticipant</c>
    ///   rows are created in `PendingPayment`.
    ///
    /// Returns 400 when the parent request is `Cancelled`/`Rejected`, when the invitee already
    /// responded, or when the caller is not the right student/guardian for the target row.
    ///
    /// Sample request body:
    /// <code>
    /// {
    ///   "data": {
    ///     "studentId": 55,
    ///     "decision": "Confirmed"
    ///   }
    /// }
    /// </code>
    /// `decision` accepts `Confirmed` or `Rejected`.
    /// </remarks>
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
}
