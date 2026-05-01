using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Payments.Commands.PayEnrollment;
using Qalam.Core.Features.Student.Payments.Commands.PayGroupMember;
using Qalam.Core.Features.Student.Payments.Queries.GetEnrollmentPaymentSummary;
using Qalam.Core.Features.Student.Payments.Queries.GetGroupEnrollmentPaymentSummary;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Mock payment endpoints for course enrollments.
/// Mock provider always succeeds — there is no real gateway.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentPaymentController : AppControllerBase
{
    /// <summary>
    /// Pay for an individual enrollment (mock — always succeeds).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/Payments/Enrollment
    ///
    /// Authorization rules:
    /// - Adult student: only the student themselves can pay.
    /// - Minor student: only the linked guardian can pay.
    ///
    /// On success the enrollment becomes Active, schedules are generated from the
    /// originating request's SelectedAvailabilities and ProposedSessions / Course.Sessions.
    ///
    /// <c>data.enrollmentId</c> must be <c>CourseEnrollment.Id</c> from GET Student/Enrollments (field <c>id</c>),
    /// not <c>CourseEnrollmentRequest.Id</c> from enrollment requests.
    /// </remarks>
    [HttpPost(Router.StudentPayEnrollment)]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayEnrollment([FromBody] PayEnrollmentCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Pay for a single member of a group enrollment (mock — always succeeds).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/Payments/GroupMember
    ///
    /// Each member (or their guardian for minors) pays their own share separately.
    /// The group becomes Active and schedules are generated when the LAST member pays.
    /// Per-member share = round(EstimatedTotalPrice / memberCount, 2). The last payer
    /// absorbs the rounding remainder so member payments sum to EstimatedTotalPrice exactly.
    /// </remarks>
    [HttpPost(Router.StudentPayGroupMember)]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayGroupMember([FromBody] PayGroupMemberCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get payment summary for an individual enrollment.
    /// </summary>
    [HttpGet(Router.StudentEnrollmentPaymentSummary)]
    [ProducesResponseType(typeof(EnrollmentPaymentSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollmentPaymentSummary(int enrollmentId)
    {
        var query = new GetEnrollmentPaymentSummaryQuery { EnrollmentId = enrollmentId };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get payment summary for a group enrollment (per-member breakdown).
    /// </summary>
    [HttpGet(Router.StudentGroupEnrollmentPaymentSummary)]
    [ProducesResponseType(typeof(GroupEnrollmentPaymentSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupEnrollmentPaymentSummary(int groupEnrollmentId)
    {
        var query = new GetGroupEnrollmentPaymentSummaryQuery { GroupEnrollmentId = groupEnrollmentId };
        return NewResult(await Mediator.Send(query));
    }
}
