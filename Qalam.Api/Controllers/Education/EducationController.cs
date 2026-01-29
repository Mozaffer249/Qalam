using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Education.Commands.CreateEducationDomain;
using Qalam.Core.Features.Education.Commands.CreateEducationLevel;
using Qalam.Core.Features.Education.Commands.CreateGrade;
using Qalam.Core.Features.Education.Commands.DeleteEducationDomain;
using Qalam.Core.Features.Education.Commands.UpdateEducationDomain;
using Qalam.Core.Features.Education.Queries.GetEducationDomainById;
using Qalam.Core.Features.Education.Queries.GetEducationDomainsList;
using Qalam.Core.Features.Education.Queries.GetFilterOptions;
using Qalam.Core.Features.Education.Queries.GetGradesList;
using Qalam.Core.Features.Education.Queries.GetLevelsList;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Education management: Domains, Levels, Grades, Terms
/// </summary>
[Authorize]
public class EducationController : AppControllerBase
{
    #region Domains

    /// <summary>
    /// Get all education domains with pagination
    /// </summary>
    [HttpGet(Router.EducationDomains)]
    public async Task<IActionResult> GetDomains([FromQuery] GetEducationDomainsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get education domain by ID
    /// </summary>
    [HttpGet(Router.EducationDomainById)]
    public async Task<IActionResult> GetDomainById(int id)
    {
        return NewResult(await Mediator.Send(new GetEducationDomainByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a new education domain (Admin only)
    /// </summary>
    [HttpPost(Router.EducationDomains)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateDomain([FromBody] CreateEducationDomainCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing education domain (Admin only)
    /// </summary>
    [HttpPut(Router.EducationDomainById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateDomain(int id, [FromBody] UpdateEducationDomainCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete an education domain (Admin only)
    /// </summary>
    [HttpDelete(Router.EducationDomainById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteDomain(int id)
    {
        return NewResult(await Mediator.Send(new DeleteEducationDomainCommand { Id = id }));
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Get filter options based on domain rules and current selection state
    /// </summary>
    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions([FromQuery] GetFilterOptionsQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    #endregion

    #region Levels

    /// <summary>
    /// Get all education levels with pagination and filters
    /// </summary>
    [HttpGet(Router.EducationLevels)]
    public async Task<IActionResult> GetLevels([FromQuery] GetLevelsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Create a new education level (Admin only)
    /// </summary>
    [HttpPost(Router.EducationLevels)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateLevel([FromBody] CreateEducationLevelCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    #endregion

    #region Grades

    /// <summary>
    /// Get all grades with pagination and filters
    /// </summary>
    [HttpGet(Router.EducationGrades)]
    public async Task<IActionResult> GetGrades([FromQuery] GetGradesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Create a new grade (Admin only)
    /// </summary>
    [HttpPost(Router.EducationGrades)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateGrade([FromBody] CreateGradeCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    #endregion
}
