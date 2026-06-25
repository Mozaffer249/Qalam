using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.CreateTeacherDomainQuestion;
using Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.DeleteTeacherDomainQuestion;
using Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.SetTeacherDomainQuestionActive;
using Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.UpdateTeacherDomainQuestion;
using Qalam.Core.Features.Admin.TeacherDomainQuestions.Queries.GetTeacherDomainQuestionById;
using Qalam.Core.Features.Admin.TeacherDomainQuestions.Queries.ListTeacherDomainQuestions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// SuperAdmin CRUD for per-domain questions shown when teachers add subjects.
/// </summary>
/// <remarks>
/// **Sample payloads:** `docs/seed-data/teacher-domain-questions.json`  
/// **Startup seed:** `TeacherDomainQuestionsSeeder` inserts questions for `school`, `quran`, and `language` domains.  
/// **Teacher read:** `GET /Api/V1/Education/Domains` (embedded `questions[]` + `requiresAnswer`).  
/// See `docs/Teacher-Domain-Questions.md`.
/// </remarks>
[ApiController]
[Route(Router.AdminTeacherDomainQuestions)]
[Authorize(Roles = Roles.SuperAdmin)]
[Tags("Admin · Teacher domain questions")]
public class TeacherDomainQuestionsController : AppControllerBase
{
    /// <summary>
    /// List domain questions, optionally filtered by education domain.
    /// </summary>
    /// <param name="domainId">Filter by domain (e.g. 1=school, 2=quran from seeded DB)</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<TeacherDomainQuestionAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int? domainId)
    {
        return NewResult(await Mediator.Send(new ListTeacherDomainQuestionsQuery { DomainId = domainId }));
    }

    /// <summary>
    /// Get a single domain question by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeacherDomainQuestionAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return NewResult(await Mediator.Send(new GetTeacherDomainQuestionByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a domain question.
    /// </summary>
    /// <param name="data">Question definition scoped to `domainId`</param>
    /// <remarks>
    /// **Sample — school text (auto-approved):**
    /// ```json
    /// {
    ///   "domainId": 1,
    ///   "code": "school_experience_years",
    ///   "nameAr": "سنوات الخبرة في التدريس",
    ///   "nameEn": "Years of teaching experience",
    ///   "requirementType": 2,
    ///   "isActive": true,
    ///   "isRequired": true,
    ///   "requiresAdminReview": false,
    ///   "sortOrder": 10,
    ///   "minCount": 1,
    ///   "maxCount": 1,
    ///   "maxLength": 100
    /// }
    /// ```
    ///
    /// **Sample — file with admin review (`requirementType`: 1 = File):**
    /// ```json
    /// {
    ///   "domainId": 2,
    ///   "code": "quran_ijaza_certificate",
    ///   "nameAr": "شهادة الإجازة",
    ///   "nameEn": "Ijaza certificate",
    ///   "requirementType": 1,
    ///   "isRequired": false,
    ///   "requiresAdminReview": true,
    ///   "sortOrder": 30,
    ///   "minCount": 0,
    ///   "maxCount": 1,
    ///   "allowedExtensions": [".pdf", ".jpg", ".jpeg", ".png"],
    ///   "mapsToDocumentType": 2
    /// }
    /// ```
    ///
    /// `code` must be unique per domain. Teachers submit via `POST /Api/V1/Teacher/DomainQuestions/submit`
    /// using `text_{code}`, `file_{code}`, `bool_{code}`, or `select_{code}` form fields.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherDomainQuestionAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTeacherDomainQuestionDto data)
    {
        return NewResult(await Mediator.Send(new CreateTeacherDomainQuestionCommand { Data = data }));
    }

    /// <summary>
    /// Update labels, flags, and validation limits (code and domainId cannot change).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeacherDomainQuestionAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTeacherDomainQuestionDto data)
    {
        return NewResult(await Mediator.Send(new UpdateTeacherDomainQuestionCommand { Id = id, Data = data }));
    }

    /// <summary>
    /// Delete a domain question.
    /// </summary>
    /// <remarks>Fails when teachers have already submitted answers. Prefer `PATCH …/{id}/active` to disable.</remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        return NewResult(await Mediator.Send(new DeleteTeacherDomainQuestionCommand { Id = id }));
    }

    /// <summary>
    /// Toggle whether a question is active (shown to teachers for that domain).
    /// </summary>
    /// <param name="data">`{ "isActive": true }`</param>
    [HttpPatch("{id:int}/active")]
    [ProducesResponseType(typeof(TeacherDomainQuestionAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetRequirementActiveDto data)
    {
        return NewResult(await Mediator.Send(new SetTeacherDomainQuestionActiveCommand { Id = id, Data = data }));
    }
}
