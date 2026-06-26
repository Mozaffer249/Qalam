using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Api.Helpers;
using Qalam.Core.Features.Teacher.Commands.SubmitTeacherDomainQuestions;
using Qalam.Core.Features.Teacher.Queries.GetTeacherDomainQuestionStatus;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher endpoints for one-time domain questionnaires before subject selection.
/// </summary>
[ApiController]
[Route("Api/V1/Teacher/DomainQuestions")]
[Authorize(Roles = Roles.Teacher)]
[Tags("Teacher · Domain questions")]
public class TeacherDomainQuestionsController : AppControllerBase
{
    /// <summary>
    /// Submit answers for all questions in an education domain (once per domain per teacher).
    /// </summary>
    /// <remarks>
    /// **Preferred — structured answers list (multipart/form-data):**
    /// ```
    /// domainId=1
    /// answers[0].code=school_experience_years
    /// answers[0].textValue=5
    /// answers[1].code=school_teaching_license
    /// answers[1].files=@license.pdf
    /// ```
    ///
    /// **Legacy (still supported when `answers` is empty):** `text_{code}`, `file_{code}`, `bool_{code}`, `select_{code}`.
    ///
    /// See `docs/Teacher-Domain-Questions.md`.
    /// </remarks>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(TeacherDomainQuestionSubmitResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromForm] SubmitTeacherDomainQuestionsCommand command)
    {
        if (!command.Answers.Any(a => !string.IsNullOrWhiteSpace(a.Code)))
        {
            command.CustomFilesByCode = TeacherRegistrationFormHelper.ParseCustomFilesByCode(Request);
            command.TextValuesByCode = TeacherRegistrationFormHelper.ParseTextValuesByCode(Request);
            command.BoolValuesByCode = TeacherRegistrationFormHelper.ParseBoolValuesByCode(Request);
            command.SelectionsByCode = TeacherRegistrationFormHelper.ParseSelectionsByCode(Request);
        }

        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Per-domain questionnaire status for the dedicated domain-questions screen (separate from subject selection).
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TeacherDomainQuestionStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus()
    {
        return NewResult(await Mediator.Send(new GetTeacherDomainQuestionStatusQuery()));
    }
}
