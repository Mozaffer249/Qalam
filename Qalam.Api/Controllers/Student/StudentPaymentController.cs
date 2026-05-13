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
    /// Individual enrollment: pay the only participant (effectively pays the whole enrollment).
    /// Group enrollment: pay one member; per-member share = round(EstimatedTotalPrice / participantCount, 2);
    /// the last payer absorbs the rounding remainder so the sum equals EstimatedTotalPrice exactly.
    ///
    /// Authorization rules:
    /// - Adult student: only the student themselves can pay.
    /// - Minor student: only the linked guardian can pay.
    ///
    /// When the LAST pending participant succeeds, the enrollment flips to Active and schedules
    /// are generated from the originating request's SelectedAvailabilities and ProposedSessions / Course.Sessions.
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
    /// Get the unified payment summary for an enrollment (individual = one participant; group = per-member breakdown).
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
