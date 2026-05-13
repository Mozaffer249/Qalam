using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.ApproveEnrollmentRequest;
using Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.RejectEnrollmentRequest;
using Qalam.Core.Features.Teacher.EnrollmentRequests.Queries.GetCourseEnrollmentRequests;
using Qalam.Core.Features.Teacher.EnrollmentRequests.Queries.GetTeacherEnrollmentRequestById;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher enrollment request management: list, view, approve, and reject **flexible-course**
/// requests. Fixed-course requests auto-approve on submit and do not require teacher action.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherEnrollmentRequests)]
public class TeacherEnrollmentRequestController : AppControllerBase
{
    /// <summary>
    /// Get enrollment requests for a course (paginated). Includes flexible requests in Pending,
    /// and any historical Approved/Rejected/Cancelled rows for both course types.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/EnrollmentRequests?CourseId=1&amp;Status=Pending&amp;PageNumber=1&amp;PageSize=10
    ///
    /// Tip: fixed-course requests rarely appear here in `Pending` — they go straight to `Approved`
    /// at submit. Use this endpoint mainly for the flexible-course review queue.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetCourseEnrollmentRequests([FromQuery] GetCourseEnrollmentRequestsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get enrollment request detail by ID. Useful for previewing the student's proposed sessions
    /// (flexible course) or slot picks (fixed course) before approving.
    /// </summary>
    /// <remarks>GET Api/V1/Teacher/EnrollmentRequests/{id}</remarks>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEnrollmentRequestById(int id)
    {
        var query = new GetTeacherEnrollmentRequestByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Approve a **flexible-course** enrollment request. Creates the unified <c>Enrollment</c>
    /// (with one or more <c>EnrollmentParticipant</c>s) in `PendingPayment` with a payment deadline.
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Teacher/EnrollmentRequests/{id}/Approve
    ///
    /// Returns 400:
    /// - When the request belongs to a **fixed course** — those auto-approve on submit and have
    ///   no `Pending` stage; teacher action is meaningless. The student should pay directly via
    ///   POST `/Student/Payments/Participants`.
    /// - When the request is not in `Pending` status (already Approved/Rejected/Cancelled).
    /// - When the request does not belong to your course.
    /// </remarks>
    [HttpPost("{id}/Approve")]
    public async Task<IActionResult> ApproveEnrollmentRequest(int id)
    {
        var command = new ApproveEnrollmentRequestCommand { RequestId = id };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Reject a flexible-course enrollment request with optional reason. The reason text is
    /// surfaced to the student verbatim in their request detail view.
    /// </summary>
    /// <remarks>POST Api/V1/Teacher/EnrollmentRequests/{id}/Reject</remarks>
    [HttpPost("{id}/Reject")]
    public async Task<IActionResult> RejectEnrollmentRequest(int id, [FromBody] RejectEnrollmentRequestDto dto)
    {
        var command = new RejectEnrollmentRequestCommand
        {
            RequestId = id,
            Data = dto
        };
        return NewResult(await Mediator.Send(command));
    }
}
