using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.CreateCourse;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.DeleteCourse;
using Qalam.Core.Features.Teacher.CourseManagement.Commands.UpdateCourse;
using Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCourseById;
using Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCoursesList;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Course;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher course management: teachers create, update, delete and list their own courses.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherCourse)]
public class TeacherCourseController : AppControllerBase
{
    /// <summary>
    /// Get paginated list of the current teacher's courses.
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/TeacherCourse
    /// Sample query: ?PageNumber=1&amp;PageSize=10&amp;DomainId=1&amp;Status=Draft&amp;SubjectId=2
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "data": {
    ///     "items": [
    ///       {
    ///         "id": 1,
    ///         "title": "Mathematics - Grade 10",
    ///         "descriptionShort": "Full curriculum algebra and geometry.",
    ///         "teacherId": 5,
    ///         "domainId": 1,
    ///         "domainNameEn": "Academic",
    ///         "subjectId": 2,
    ///         "subjectNameEn": "Mathematics",
    ///         "teachingModeId": 1,
    ///         "teachingModeNameEn": "Online",
    ///         "sessionTypeId": 1,
    ///         "sessionTypeNameEn": "Group",
    ///         "status": "Draft",
    ///         "isActive": true,
    ///         "price": 500.00
    ///       }
    ///     ],
    ///     "pageNumber": 1,
    ///     "pageSize": 10,
    ///     "totalCount": 5
    ///   },
    ///   "succeeded": true
    /// }
    /// </code>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] GetCoursesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get course by ID (own course only).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/TeacherCourse/{id}
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "data": {
    ///     "id": 1,
    ///     "title": "Mathematics - Grade 10",
    ///     "description": "Full curriculum algebra and geometry.",
    ///     "isActive": true,
    ///     "teacherId": 5,
    ///     "teacherDisplayName": "Ahmed Ali",
    ///     "domainId": 1,
    ///     "domainNameEn": "Academic",
    ///     "teacherSubjectId": 1,
    ///     "subjectNameEn": "Mathematics",
    ///     "curriculumId": 1,
    ///     "curriculumNameEn": "Saudi National",
    ///     "levelId": 2,
    ///     "levelNameEn": "Secondary",
    ///     "gradeId": 4,
    ///     "gradeNameEn": "Grade 10",
    ///     "teachingModeId": 1,
    ///     "teachingModeNameEn": "Online",
    ///     "sessionTypeId": 1,
    ///     "sessionTypeNameEn": "Group",
    ///     "isFlexible": false,
    ///     "sessionsCount": 3,
    ///     "sessionDurationMinutes": 45,
    ///     "price": 500.00,
    ///     "maxStudents": 15,
    ///     "canIncludeInPackages": true,
    ///     "status": "Published",
    ///     "units": null,
    ///     "sessions": [
    ///       { "id": 101, "sessionNumber": 1, "durationMinutes": 45, "title": "Intro",      "notes": null },
    ///       { "id": 102, "sessionNumber": 2, "durationMinutes": 45, "title": "Equations",  "notes": null },
    ///       { "id": 103, "sessionNumber": 3, "durationMinutes": 60, "title": "Quadratics", "notes": "Bring calculator." }
    ///     ]
    ///   },
    ///   "succeeded": true
    /// }
    /// </code>
    /// `sessionsCount` is derived from `sessions.length` on read (null for flexible courses).
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var query = new GetCourseByIdQuery { Id = id };
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Create a new course (Published).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Teacher/TeacherCourse
    ///
    /// Two modes:
    /// - Non-flexible (`isFlexible = false`): `sessionDurationMinutes` is required, and `sessions` must be a non-empty list. The array order is the session order — the server assigns `sessionNumber = 1..N`. Do NOT send `sessionNumber`.
    /// - Flexible (`isFlexible = true`): `sessionDurationMinutes` must be null and `sessions` must be null/empty.
    ///
    /// Sample request body (non-flexible):
    /// <code>
    /// {
    ///   "title": "Mathematics - Grade 10",
    ///   "description": "Full curriculum algebra and geometry.",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 1,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": false,
    ///   "sessionDurationMinutes": 45,
    ///   "price": 500,
    ///   "maxStudents": 15,
    ///   "canIncludeInPackages": true,
    ///   "sessions": [
    ///     { "durationMinutes": 45, "title": "Intro",      "notes": null },
    ///     { "durationMinutes": 45, "title": "Equations",  "notes": null },
    ///     { "durationMinutes": 60, "title": "Quadratics", "notes": "Bring calculator." }
    ///   ]
    /// }
    /// </code>
    ///
    /// Sample request body (flexible):
    /// <code>
    /// {
    ///   "title": "On-demand Tutoring",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 2,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": true,
    ///   "sessionDurationMinutes": null,
    ///   "price": 40,
    ///   "maxStudents": null,
    ///   "canIncludeInPackages": false,
    ///   "sessions": null
    /// }
    /// </code>
    ///
    /// On success returns the full <see cref="CourseDetailDto"/> with `status = Published` and `sessions[]` ordered by `sessionNumber` ascending.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var command = new CreateCourseCommand { Data = dto };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing course.
    /// </summary>
    /// <remarks>
    /// PUT Api/V1/Teacher/TeacherCourse/{id}
    ///
    /// Sample request body:
    /// <code>
    /// {
    ///   "title": "Mathematics - Grade 10 (Updated)",
    ///   "description": "Updated curriculum with new topics.",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 1,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": false,
    ///   "sessionDurationMinutes": 60,
    ///   "price": 600,
    ///   "maxStudents": 20,
    ///   "canIncludeInPackages": true
    /// }
    /// </code>
    ///
    /// Note: this endpoint does not edit `sessions[]`. Use the dedicated session management endpoints to change the session outline.
    /// </remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
    {
        var command = new UpdateCourseCommand { Id = id, Data = dto };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a course (soft if has enrollments, hard otherwise).
    /// </summary>
    /// <remarks>
    /// DELETE Api/V1/Teacher/TeacherCourse/{id}
    ///
    /// Soft-deletes if course has enrollments (sets IsActive = false), hard-deletes otherwise.
    ///
    /// Sample response:
    /// <code>
    /// {
    ///   "succeeded": true,
    ///   "message": "Deleted Successfully"
    /// }
    /// </code>
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var cmd = new DeleteCourseCommand { Id = id };
        return NewResult(await Mediator.Send(cmd));
    }
}
