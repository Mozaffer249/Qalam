using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Payments.Commands.ConfirmPayment;
using Qalam.Core.Features.Student.Payments.Commands.CreatePaymentIntent;
using Qalam.Core.Features.Student.Payments.Commands.PayEnrollmentParticipant;
using Qalam.Core.Features.Student.Payments.Queries.GetEnrollmentPaymentSummary;
using Qalam.Core.Features.Student.Payments.Queries.GetMyPayments;
using Qalam.Core.Features.Student.Payments.Queries.GetPaymentReceipt;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Student enrollment payments. Mock/free-trial use Participants; card gateways use Intents + Confirm.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentPaymentController : AppControllerBase
{
    /// <summary>
    /// Pay one participant (mock provider or free-trial zero amount).
    /// </summary>
    [HttpPost(Router.StudentPayEnrollmentParticipant)]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayEnrollmentParticipant([FromBody] PayEnrollmentParticipantCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Create a payment intent (Pending payment + checkout payload for the Flutter client).
    /// </summary>
    [HttpPost(Router.StudentPaymentIntent)]
    [ProducesResponseType(typeof(PaymentIntentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Confirm a payment after the SDK / hosted return reports paid.
    /// </summary>
    [HttpPost(Router.StudentPaymentConfirm)]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get the unified payment summary for an enrollment.
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
    /// Paginated payment history for the authenticated payer.
    /// </summary>
    [HttpGet(Router.StudentPayments)]
    [ProducesResponseType(typeof(List<StudentPaymentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPayments([FromQuery] GetMyPaymentsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Payment receipt detail for the authenticated payer.
    /// </summary>
    [HttpGet(Router.StudentPaymentById)]
    [ProducesResponseType(typeof(StudentPaymentReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentReceipt(int paymentId)
    {
        var query = new GetPaymentReceiptQuery { PaymentId = paymentId };
        return NewResult(await Mediator.Send(query));
    }
}
