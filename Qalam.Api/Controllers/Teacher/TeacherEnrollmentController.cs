using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetCourseEnrollmentsList;
using Qalam.Core.Features.Teacher.Enrollments.Queries.GetTeacherEnrollmentById;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher views of post-approval enrollments — unified list per course, plus per-enrollment
/// detail (individual or group) including the generated CourseSchedule sessions.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherEnrollmentController : AppControllerBase
{
    /// <summary>
    /// Get all enrollments for a course (unified — kind tells individual vs group apart).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/Courses/{courseId}/Enrollments?Status=Active&amp;PageNumber=1&amp;PageSize=20
    ///
    /// Each row carries a `kind` discriminator (`Individual` or `Group`) plus a `participantCount`.
    /// Use the row's `id` with GET `/Teacher/Enrollments/{id}` for full detail (works for both kinds).
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
    /// Enrollment detail — unified for individual and group. Returns participants[] (length 1 for individual, N for group)
    /// plus the generated session schedule.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/Enrollments/{id}
    ///
    /// `sessions[]` is empty until the enrollment is `Active` (all participants paid). Each session carries
    /// `scheduleId`, `date`, `title`, `startTime`/`endTime`, `durationMinutes`, `status`, and a `canStart` flag
    /// (true when current UTC is inside the session window).
    /// </remarks>
    [HttpGet(Router.TeacherEnrollmentById)]
    [ProducesResponseType(typeof(TeacherEnrollmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollmentById(int id)
    {
        var query = new GetTeacherEnrollmentByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }
}
