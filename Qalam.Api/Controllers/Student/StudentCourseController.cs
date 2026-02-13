using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;
using Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCoursesList;
using Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;
using Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequestById;
using Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequests;
using Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollmentById;
using Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Student course catalog, enrollment requests, and my enrollments.
/// </summary>
[Authorize(Roles = Roles.Student)]
[ApiController]
[Route("Api/V1/Student")]
public class StudentCourseController : AppControllerBase
{
    /// <summary>
    /// Get paginated list of published courses (catalog). When the student has DomainId, CurriculumId, LevelId, or GradeId set, results are filtered to compatible courses; query params override. UserId is set from JWT.
    /// </summary>
    /// <remarks>GET Api/V1/Student/Courses?PageNumber=1&amp;PageSize=10&amp;DomainId=1&amp;CurriculumId=2&amp;LevelId=3&amp;GradeId=4&amp;SubjectId=5&amp;TeachingModeId=1</remarks>
    [HttpGet("Courses")]
    [ProducesResponseType(typeof(PaginatedResult<CourseCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedCourses([FromQuery] GetPublishedCoursesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get a published course by ID.
    /// </summary>
    /// <remarks>GET Api/V1/Student/Courses/{id}</remarks>
    [HttpGet("Courses/{id}")]
    [ProducesResponseType(typeof(CourseCatalogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedCourseById(int id)
    {
        var query = new GetPublishedCourseByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Request enrollment in a course.
    /// </summary>
    /// <remarks>POST Api/V1/Student/EnrollmentRequests</remarks>
    [HttpPost("EnrollmentRequests")]
    [ProducesResponseType(typeof(EnrollmentRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestEnrollment([FromBody] RequestCourseEnrollmentCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get my enrollment requests (paginated).
    /// </summary>
    /// <remarks>GET Api/V1/Student/EnrollmentRequests</remarks>
    [HttpGet("EnrollmentRequests")]
    [ProducesResponseType(typeof(PaginatedResult<EnrollmentRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollmentRequests([FromQuery] GetMyEnrollmentRequestsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get my enrollment request by ID.
    /// </summary>
    /// <remarks>GET Api/V1/Student/EnrollmentRequests/{id}</remarks>
    [HttpGet("EnrollmentRequests/{id}")]
    [ProducesResponseType(typeof(EnrollmentRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEnrollmentRequestById(int id)
    {
        var query = new GetMyEnrollmentRequestByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get my enrollments (paginated).
    /// </summary>
    /// <remarks>GET Api/V1/Student/Enrollments</remarks>
    [HttpGet("Enrollments")]
    [ProducesResponseType(typeof(PaginatedResult<EnrollmentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollments([FromQuery] GetMyEnrollmentsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get my enrollment by ID.
    /// </summary>
    /// <remarks>GET Api/V1/Student/Enrollments/{id}</remarks>
    [HttpGet("Enrollments/{id}")]
    [ProducesResponseType(typeof(EnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEnrollmentById(int id)
    {
        var query = new GetMyEnrollmentByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }
}
