using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Content.Commands.CreateContentUnit;
using Qalam.Core.Features.Content.Commands.CreateLesson;
using Qalam.Core.Features.Content.Queries.GetContentUnitsList;
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
    /// Create a new content unit (Admin only)
    /// </summary>
    [HttpPost(Router.ContentUnits)]
    [Authorize(Roles = "Admin,SuperAdmin,Teacher")]
    public async Task<IActionResult> CreateContentUnit([FromBody] CreateContentUnitCommand command)
    {
        return NewResult(await Mediator.Send(command));
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
    /// Create a new lesson (Admin or Teacher)
    /// </summary>
    [HttpPost(Router.ContentLessons)]
    [Authorize(Roles = "Admin,SuperAdmin,Teacher")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    #endregion
}
