using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Curriculum.Commands.CreateCurriculum;
using Qalam.Core.Features.Curriculum.Commands.DeleteCurriculum;
using Qalam.Core.Features.Curriculum.Commands.ToggleCurriculumStatus;
using Qalam.Core.Features.Curriculum.Commands.UpdateCurriculum;
using Qalam.Core.Features.Curriculum.Queries.GetCurriculumById;
using Qalam.Core.Features.Curriculum.Queries.GetCurriculumsList;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Curriculum management: List and manage curriculums
/// </summary>
[Authorize]
public class CurriculumController : AppControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all curriculums with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="search">Search term for filtering by name</param>
    /// <returns>Paginated list of curriculums</returns>
    [HttpGet(Router.Curriculum)]
    public async Task<IActionResult> GetCurriculums(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = new GetCurriculumsListQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search
        };
        var result = await _mediator.Send(query);
        return NewResult(result);
    }

    /// <summary>
    /// Get curriculum by ID
    /// </summary>
    /// <param name="id">Curriculum ID</param>
    /// <returns>Curriculum details</returns>
    [HttpGet(Router.CurriculumById)]
    public async Task<IActionResult> GetCurriculumById(int id)
    {
        var query = new GetCurriculumByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        return NewResult(result);
    }

    /// <summary>
    /// Create a new curriculum
    /// </summary>
    /// <param name="command">Curriculum creation data</param>
    /// <returns>Created curriculum</returns>
    [HttpPost(Router.Curriculum)]
    public async Task<IActionResult> CreateCurriculum([FromBody] CreateCurriculumCommand command)
    {
        var result = await _mediator.Send(command);
        return NewResult(result);
    }

    /// <summary>
    /// Update an existing curriculum
    /// </summary>
    /// <param name="id">Curriculum ID</param>
    /// <param name="command">Curriculum update data</param>
    /// <returns>Updated curriculum</returns>
    [HttpPut(Router.CurriculumById)]
    public async Task<IActionResult> UpdateCurriculum(int id, [FromBody] UpdateCurriculumCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch between route and body");

        var result = await _mediator.Send(command);
        return NewResult(result);
    }

    /// <summary>
    /// Delete a curriculum
    /// </summary>
    /// <param name="id">Curriculum ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete(Router.CurriculumById)]
    public async Task<IActionResult> DeleteCurriculum(int id)
    {
        var command = new DeleteCurriculumCommand { Id = id };
        var result = await _mediator.Send(command);
        return NewResult(result);
    }

    /// <summary>
    /// Toggle curriculum active status
    /// </summary>
    /// <param name="id">Curriculum ID</param>
    /// <returns>Toggle result</returns>
    [HttpPatch(Router.CurriculumById + "/toggle-status")]
    public async Task<IActionResult> ToggleCurriculumStatus(int id)
    {
        var command = new ToggleCurriculumStatusCommand { Id = id };
        var result = await _mediator.Send(command);
        return NewResult(result);
    }
}
