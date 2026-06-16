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
    /// Education filter wizard — next step and options from cumulative query selections.
    /// </summary>
    /// <remarks>
    /// Stateless: send all IDs chosen so far on every call. After **Grade** → **Subject** → **Term** → **Unit** → optional **Lesson** → **Done**.
    /// See OpenAPI description on this operation and `Qalam.Data/AppMetaData/docs/Education_Business_Logic.md`.
    /// </remarks>
    /// <param name="domainId">Required. Education domain ID — start here (`GET /Education/Domains`).</param>
    /// <param name="curriculumId">Wizard step 2 — after `nextStep` was `Curriculum`.</param>
    /// <param name="levelId">Wizard step 3 — after `nextStep` was `Level`.</param>
    /// <param name="gradeId">Wizard step 4 — after `nextStep` was `Grade`. Next step is Subject.</param>
    /// <param name="subjectId">Wizard step 5 — after `nextStep` was `Subject`. Send before termIds.</param>
    /// <param name="termIds">Wizard step 6 — after `nextStep` was `Term`. Repeat param for multi-select (`termIds=1&amp;termIds=2`).</param>
    /// <param name="contentUnitId">Wizard step 7 — after picking from `data.unit[]` when `nextStep` was `Unit`.</param>
    /// <param name="lessonIds">Wizard step 8 — optional lesson multi-select after `Unit`. Repeat param.</param>
    /// <param name="skipLessons">When true with contentUnitId, skip Lesson step and return Done.</param>
    /// <param name="quranContentTypeId">Quran domain only (echo / client state).</param>
    /// <param name="quranLevelId">Quran domain only (echo / client state).</param>
    /// <param name="unitTypeCode">Quran domain: `QuranPart` (default) or `QuranSurah`.</param>
    /// <param name="pageNumber">Pagination when `nextStep` is `Unit` (Quran).</param>
    /// <param name="pageSize">Pagination when `nextStep` is `Unit` (Quran).</param>
    [HttpGet(Router.Education + "/filter-options")]
    [ProducesResponseType(typeof(Qalam.Data.DTOs.FilterOptionsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterOptions(
        [FromQuery] int domainId,
        [FromQuery] int? curriculumId,
        [FromQuery] int? levelId,
        [FromQuery] int? gradeId,
        [FromQuery] int? subjectId,
        [FromQuery] List<int>? termIds,
        [FromQuery] int? contentUnitId,
        [FromQuery] List<int>? lessonIds,
        [FromQuery] int? quranContentTypeId,
        [FromQuery] int? quranLevelId,
        [FromQuery] string? unitTypeCode,
        [FromQuery] bool skipLessons = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetFilterOptionsQuery
        {
            DomainId = domainId,
            CurriculumId = curriculumId,
            LevelId = levelId,
            GradeId = gradeId,
            SubjectId = subjectId,
            TermIds = termIds,
            ContentUnitId = contentUnitId,
            LessonIds = lessonIds,
            SkipLessons = skipLessons,
            QuranContentTypeId = quranContentTypeId,
            QuranLevelId = quranLevelId,
            UnitTypeCode = unitTypeCode,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
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
