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
/// Teacher enrollment request management: view, approve, and reject enrollment requests for courses.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherEnrollmentRequests)]
public class TeacherEnrollmentRequestController : AppControllerBase
{
    /// <summary>
    /// Get enrollment requests for a specific course (paginated).
    /// </summary>
    /// <remarks>GET Api/V1/Teacher/EnrollmentRequests?CourseId=1&amp;Status=Pending&amp;PageNumber=1&amp;PageSize=10</remarks>
    [HttpGet]
    public async Task<IActionResult> GetCourseEnrollmentRequests([FromQuery] GetCourseEnrollmentRequestsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get enrollment request detail by ID.
    /// </summary>
    /// <remarks>GET Api/V1/Teacher/EnrollmentRequests/{id}</remarks>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEnrollmentRequestById(int id)
    {
        var query = new GetTeacherEnrollmentRequestByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Approve an enrollment request. Creates enrollment with payment deadline.
    /// </summary>
    /// <remarks>POST Api/V1/Teacher/EnrollmentRequests/{id}/Approve</remarks>
    [HttpPost("{id}/Approve")]
    public async Task<IActionResult> ApproveEnrollmentRequest(int id)
    {
        var command = new ApproveEnrollmentRequestCommand { RequestId = id };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Reject an enrollment request with optional reason.
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
