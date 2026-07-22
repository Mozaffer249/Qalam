using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Education.Commands.CreateAcademicTerm;
using Qalam.Core.Features.Education.Commands.CreateEducationDomain;
using Qalam.Core.Features.Education.Commands.CreateEducationLevel;
using Qalam.Core.Features.Education.Commands.CreateGrade;
using Qalam.Core.Features.Education.Commands.DeleteAcademicTerm;
using Qalam.Core.Features.Education.Commands.DeleteEducationDomain;
using Qalam.Core.Features.Education.Commands.DeleteEducationLevel;
using Qalam.Core.Features.Education.Commands.DeleteGrade;
using Qalam.Core.Features.Education.Commands.UpdateAcademicTerm;
using Qalam.Core.Features.Education.Commands.ToggleEducationDomainStatus;
using Qalam.Core.Features.Education.Commands.UpdateEducationDomain;
using Qalam.Core.Features.Education.Commands.UpdateEducationLevel;
using Qalam.Core.Features.Education.Commands.UpdateGrade;
using Qalam.Core.Features.Education.Queries.GetAcademicTermById;
using Qalam.Core.Features.Education.Queries.GetAcademicTermsList;
using Qalam.Core.Features.Education.Queries.GetEducationDomainById;
using Qalam.Core.Features.Education.Queries.GetEducationDomainsList;
using Qalam.Core.Features.Education.Queries.GetEducationLevelById;
using Qalam.Core.Features.Education.Queries.GetFilterOptions;
using Qalam.Core.Features.Education.Queries.GetGradeById;
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

    /// <summary>
    /// Toggle education domain active status
    /// </summary>
    [HttpPatch(Router.EducationDomainById + "/toggle-status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ToggleDomainStatus(int id)
    {
        return NewResult(await Mediator.Send(new ToggleEducationDomainStatusCommand { Id = id }));
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Education filter wizard — next step and options from cumulative query selections.
    /// </summary>
    /// <remarks>
    /// Stateless: send all IDs chosen so far on every call.
    /// School: Curriculum → Level → Grade → Subject → Term → Unit → optional Lesson → Done.
    /// University: University → College → Department → AcademicProgram → Level → Subject → [Term?] → Unit → Lesson → Done.
    /// Bind via <see cref="GetFilterOptionsQuery"/> query string (DomainId, CurriculumId, UniversityId, CollegeId, DepartmentId, AcademicProgramId, SkipTerm, etc.).
    /// See OpenAPI description on this operation and `Qalam.Data/AppMetaData/docs/Education_Business_Logic.md`.
    /// </remarks>
    /// <param name="query">Cumulative filter state from the query string.</param>
    [HttpGet(Router.Education + "/filter-options")]
    [ProducesResponseType(typeof(Qalam.Data.DTOs.FilterOptionsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterOptions([FromQuery] GetFilterOptionsQuery query)
        => NewResult(await Mediator.Send(query));

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
    /// Get education level by ID
    /// </summary>
    [HttpGet(Router.EducationLevelById)]
    public async Task<IActionResult> GetLevelById(int id)
    {
        return NewResult(await Mediator.Send(new GetEducationLevelByIdQuery { Id = id }));
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

    /// <summary>
    /// Update an existing education level (Admin only)
    /// </summary>
    [HttpPut(Router.EducationLevelById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateLevel(int id, [FromBody] UpdateEducationLevelCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete an education level (Admin only)
    /// </summary>
    [HttpDelete(Router.EducationLevelById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteLevel(int id)
    {
        return NewResult(await Mediator.Send(new DeleteEducationLevelCommand { Id = id }));
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
    /// Get grade by ID
    /// </summary>
    [HttpGet(Router.EducationGradeById)]
    public async Task<IActionResult> GetGradeById(int id)
    {
        return NewResult(await Mediator.Send(new GetGradeByIdQuery { Id = id }));
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

    /// <summary>
    /// Update an existing grade
    /// </summary>
    [HttpPut(Router.EducationGradeById)]
    public async Task<IActionResult> UpdateGrade(int id, [FromBody] UpdateGradeCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a grade
    /// </summary>
    [HttpDelete(Router.EducationGradeById)]
    public async Task<IActionResult> DeleteGrade(int id)
    {
        return NewResult(await Mediator.Send(new DeleteGradeCommand { Id = id }));
    }

    #endregion

    #region Terms

    /// <summary>
    /// Get all academic terms with pagination and filters
    /// </summary>
    [HttpGet(Router.EducationTerms)]
    public async Task<IActionResult> GetTerms([FromQuery] GetAcademicTermsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get academic term by ID
    /// </summary>
    [HttpGet(Router.EducationTermById)]
    public async Task<IActionResult> GetTermById(int id)
    {
        return NewResult(await Mediator.Send(new GetAcademicTermByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a new academic term (Admin only)
    /// </summary>
    [HttpPost(Router.EducationTerms)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateTerm([FromBody] CreateAcademicTermCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing academic term
    /// </summary>
    [HttpPut(Router.EducationTermById)]
    public async Task<IActionResult> UpdateTerm(int id, [FromBody] UpdateAcademicTermCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete an academic term
    /// </summary>
    [HttpDelete(Router.EducationTermById)]
    public async Task<IActionResult> DeleteTerm(int id)
    {
        return NewResult(await Mediator.Send(new DeleteAcademicTermCommand { Id = id }));
    }

    #endregion
}
