using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Content.Commands.CreateContentUnit;
using Qalam.Core.Features.Content.Commands.CreateLesson;
using Qalam.Core.Features.Content.Commands.DeleteContentUnit;
using Qalam.Core.Features.Content.Commands.DeleteLesson;
using Qalam.Core.Features.Content.Commands.UpdateContentUnit;
using Qalam.Core.Features.Content.Commands.UpdateLesson;
using Qalam.Core.Features.Content.Queries.GetContentUnitById;
using Qalam.Core.Features.Content.Queries.GetContentUnitsList;
using Qalam.Core.Features.Content.Queries.GetLessonById;
using Qalam.Core.Features.Content.Queries.GetLessonsList;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Content management: Units and Lessons
/// </summary>
[Authorize]
public class ContentController : AppControllerBase
{
    #region Content Units

    /// <summary>
    /// Get all content units with pagination and filters
    /// </summary>
    [HttpGet(Router.ContentUnits)]
    public async Task<IActionResult> GetContentUnits([FromQuery] GetContentUnitsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get content unit by ID
    /// </summary>
    [HttpGet(Router.ContentUnitById)]
    public async Task<IActionResult> GetContentUnitById(int id)
    {
        return NewResult(await Mediator.Send(new GetContentUnitByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a new content unit (Admin only)
    /// </summary>
    [HttpPost(Router.ContentUnits)]
    [Authorize(Roles = "Admin,SuperAdmin,Teacher")]
    public async Task<IActionResult> CreateContentUnit([FromBody] CreateContentUnitCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing content unit
    /// </summary>
    [HttpPut(Router.ContentUnitById)]
    [Authorize(Roles = "Admin,SuperAdmin,Teacher")]
    public async Task<IActionResult> UpdateContentUnit(int id, [FromBody] UpdateContentUnitCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a content unit (Admin only)
    /// </summary>
    [HttpDelete(Router.ContentUnitById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteContentUnit(int id)
    {
        return NewResult(await Mediator.Send(new DeleteContentUnitCommand { Id = id }));
    }

    #endregion

    #region Lessons

    /// <summary>
    /// Get all lessons with pagination and filters
    /// </summary>
    [HttpGet(Router.ContentLessons)]
    public async Task<IActionResult> GetLessons([FromQuery] GetLessonsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get lesson by ID
    /// </summary>
    [HttpGet(Router.ContentLessonById)]
    public async Task<IActionResult> GetLessonById(int id)
    {
        return NewResult(await Mediator.Send(new GetLessonByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a new lesson (Admin or Teacher)
    /// </summary>
    [HttpPost(Router.ContentLessons)]
    [Authorize(Roles = "Admin,SuperAdmin,Teacher")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing lesson
    /// </summary>
    [HttpPut(Router.ContentLessonById)]
    public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a lesson
    /// </summary>
    [HttpDelete(Router.ContentLessonById)]
    public async Task<IActionResult> DeleteLesson(int id)
    {
        return NewResult(await Mediator.Send(new DeleteLessonCommand { Id = id }));
    }

    #endregion
}
