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
[Route("Api/V1/Teacher/[controller]")]
public class TeacherCourseController : AppControllerBase
{
    /// <summary>
    /// Get paginated list of the current teacher's courses.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] GetCoursesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get course by ID (own course only).
    /// </summary>
    [HttpGet(Router.SingleRoute)]
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
    [HttpPut(Router.SingleRoute)]
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
    [HttpDelete(Router.SingleRoute)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var cmd = new DeleteCourseCommand { Id = id };
        return NewResult(await Mediator.Send(cmd));
    }
}
