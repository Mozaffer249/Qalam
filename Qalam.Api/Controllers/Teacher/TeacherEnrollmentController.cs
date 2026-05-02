using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetCourseEnrollmentsList;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentById;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherGroupEnrollmentById;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher views of post-approval enrollments — list per course, plus per-enrollment
/// detail (individual + group) including the generated CourseSchedule sessions.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherEnrollmentController : AppControllerBase
{
    /// <summary>
    /// Get all enrollments for a course (mixes individual and group, both pending-payment and active).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/Courses/{courseId}/Enrollments?Status=Active&amp;PageNumber=1&amp;PageSize=20
    ///
    /// Each row carries a `kind` discriminator (`Individual` or `Group`). Use the row's `id`
    /// with the appropriate detail endpoint:
    /// - Individual → GET `/Teacher/Enrollments/{id}`
    /// - Group → GET `/Teacher/GroupEnrollments/{id}`
    /// </remarks>
    [HttpGet(Router.TeacherCourseEnrollments)]
    [ProducesResponseType(typeof(List<TeacherEnrollmentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseEnrollments(int courseId, [FromQuery] GetCourseEnrollmentsListQuery query)
    {
        query.CourseId = courseId;
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Individual enrollment detail with payment status and the generated session schedule.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/Enrollments/{id}
    ///
    /// `sessions[]` is empty until the student has paid and the enrollment is `Active`. Each
    /// session carries `scheduleId`, `date`, `title`, `startTime`/`endTime`, `durationMinutes`,
    /// `status`, and a `canStart` flag (true when current UTC is inside the session window).
    /// </remarks>
    [HttpGet(Router.TeacherEnrollmentById)]
    [ProducesResponseType(typeof(TeacherEnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollmentById(int id)
    {
        var query = new GetTeacherEnrollmentByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Group enrollment detail with per-member payment breakdown and the generated session schedule.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/GroupEnrollments/{id}
    ///
    /// `sessions[]` is empty until ALL members have paid and the group is `Active`.
    /// `members[]` lists each student with their `paymentStatus`, `paidAt`, `memberType`,
    /// and resolved `share`.
    /// </remarks>
    [HttpGet(Router.TeacherGroupEnrollmentById)]
    [ProducesResponseType(typeof(TeacherGroupEnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupEnrollmentById(int id)
    {
        var query = new GetTeacherGroupEnrollmentByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }
}
