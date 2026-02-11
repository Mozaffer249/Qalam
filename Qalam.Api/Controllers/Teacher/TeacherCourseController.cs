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
using Qalam.Data.Results;

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
    /// Sample query: ?PageNumber=1&amp;PageSize=10&amp;DomainId=1&amp;Status=0&amp;SubjectId=2
    /// Sample response: { "data": { "items": [...], "pageNumber": 1, "pageSize": 10, "totalCount": 5 }, "succeeded": true }
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] GetCoursesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get course by ID (own course only).
    /// </summary>
    /// <remarks>
    /// GET Api/V1/Teacher/TeacherCourse/{id}
    /// Sample: GET Api/V1/Teacher/TeacherCourse/1
    /// Sample response: { "data": { "id": 1, "title": "Math Grade 10", "description": "...", "price": 500, "status": 0, ... }, "succeeded": true }
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
    /// Create a new course (Draft).
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Teacher/TeacherCourse
    /// Sample request body:
    /// <code>
    /// {
    ///   "title": "Mathematics - Grade 10",
    ///   "description": "Full curriculum algebra and geometry.",
    ///   "teacherSubjectId": 1,
    ///   "teachingModeId": 1,
    ///   "sessionTypeId": 1,
    ///   "isFlexible": false,
    ///   "sessionsCount": 12,
    ///   "sessionDurationMinutes": 45,
    ///   "price": 500,
    ///   "maxStudents": 15,
    ///   "canIncludeInPackages": true
    /// }
    /// </code>
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
    /// Sample: PUT Api/V1/Teacher/TeacherCourse/1
    /// Sample request body: same as Create (title, description, teacherSubjectId, teachingModeId, sessionTypeId, isFlexible, sessionsCount, sessionDurationMinutes, price, maxStudents, canIncludeInPackages).
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
    /// <remarks>DELETE Api/V1/Teacher/TeacherCourse/{id}. Sample: DELETE Api/V1/Teacher/TeacherCourse/1</remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var cmd = new DeleteCourseCommand { Id = id };
        return NewResult(await Mediator.Send(cmd));
    }
}
