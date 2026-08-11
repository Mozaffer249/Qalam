using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Payments.Commands.PayEnrollmentParticipant;
using Qalam.Core.Features.Student.Payments.Queries.GetEnrollmentPaymentSummary;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Mock payment endpoints for enrollment participants.
/// Mock provider always succeeds — there is no real gateway.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentPaymentController : AppControllerBase
{
    /// <summary>
    /// Pay one participant of an enrollment (mock — always succeeds).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/Payments/Participants
    ///
    /// Single-payer model: only the enrollment owner (request creator / Enrollment.OwnerUserId)
    /// may pay. One successful payment covers the full <c>AmountDue</c> and marks all participants Succeeded.
    ///
    /// Authorization: caller must be the enrollment owner (not per-invitee / not child of invitee).
    ///
    /// On success the enrollment flips to Active and schedules are generated from the originating
    /// request's SelectedAvailabilities and ProposedSessions / Course.Sessions (or OSR offer slots).
    /// </remarks>
    [HttpPost(Router.StudentPayEnrollmentParticipant)]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayEnrollmentParticipant([FromBody] PayEnrollmentParticipantCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get the unified payment summary for an enrollment (owner pays full amount; participants show status).
    /// </summary>
    [HttpGet(Router.StudentEnrollmentPaymentSummary)]
    [ProducesResponseType(typeof(EnrollmentPaymentSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollmentPaymentSummary(int enrollmentId)
    {
        var query = new GetEnrollmentPaymentSummaryQuery { EnrollmentId = enrollmentId };
        return NewResult(await Mediator.Send(query));
    }
}
