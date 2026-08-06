using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Commands.DeleteTeacherSubject;
using Qalam.Core.Features.Teacher.Commands.SaveTeacherSubjects;
using Qalam.Core.Features.Teacher.Commands.UpdateTeacherSubject;
using Qalam.Core.Features.Teacher.Commands.UpdateTeacherSubjectBySubject;
using Qalam.Core.Features.Teacher.Queries.GetTeacherSubjects;
using Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnitOptions;
using Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnitOptionsBySubject;
using Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnits;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher endpoints for managing subjects and teaching units
/// </summary>
[ApiController]
[Route("Api/V1/Teacher/[controller]")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherSubjectController : AppControllerBase
{
    /// <summary>
    /// Get all subjects with units for the current teacher
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TeacherSubjectResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherSubjects()
    {
        return NewResult(await Mediator.Send(new GetTeacherSubjectsQuery()));
    }

    /// <summary>
    /// Active repertoire units for a teacher subject (course-create picker).
    /// Prefer <c>unit-options</c> for profile editing (full catalog + IsSelected).
    /// </summary>
    [HttpGet("{teacherSubjectId:int}/units")]
    [ProducesResponseType(typeof(List<TeacherSubjectUnitOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherSubjectUnits(int teacherSubjectId)
    {
        return NewResult(await Mediator.Send(new GetTeacherSubjectUnitsQuery
        {
            TeacherSubjectId = teacherSubjectId,
        }));
    }

    /// <summary>
    /// Full active catalog with IsSelected flags for profile edit (owner-only; any verification status).
    /// </summary>
    [HttpGet("{teacherSubjectId:int}/unit-options")]
    [ProducesResponseType(typeof(List<TeacherSubjectUnitPickerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherSubjectUnitOptions(int teacherSubjectId)
    {
        return NewResult(await Mediator.Send(new GetTeacherSubjectUnitOptionsQuery
        {
            TeacherSubjectId = teacherSubjectId,
        }));
    }

    /// <summary>
    /// Full active catalog for a subject with IsSelected flags (profile edit drawer).
    /// Keyed by catalog SubjectId (unique per teacher). Same semantics as
    /// <see cref="GetTeacherSubjectUnitOptions"/> but resolved via SubjectId.
    /// </summary>
    [HttpGet("Subject/{subjectId:int}/unit-options")]
    [ProducesResponseType(typeof(List<TeacherSubjectUnitPickerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherSubjectUnitOptionsBySubject(int subjectId)
    {
        return NewResult(await Mediator.Send(new GetTeacherSubjectUnitOptionsBySubjectQuery
        {
            SubjectId = subjectId,
        }));
    }

    /// <summary>
    /// Save teacher subjects with units (batch — adds/updates by SubjectId).
    /// </summary>
    /// <remarks>
    /// For Quran domain, send coverage sets at subject level:
    /// - quranContentTypeIds: empty = all types
    /// - quranLevelIds: empty = all levels
    /// Units are plain unitIds only (no per-unit type/level).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherSubjectsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveTeacherSubjects([FromBody] SaveTeacherSubjectsDto dto)
    {
        var command = new SaveTeacherSubjectsCommand
        {
            Subjects = dto.Subjects
        };

        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update units / CanTeachFullSubject / Quran coverage keyed by catalog SubjectId.
    /// </summary>
    [HttpPut("Subject/{subjectId:int}")]
    [ProducesResponseType(typeof(TeacherSubjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeacherSubjectBySubject(int subjectId, [FromBody] UpdateTeacherSubjectDto dto)
    {
        return NewResult(await Mediator.Send(new UpdateTeacherSubjectBySubjectCommand
        {
            SubjectId = subjectId,
            CanTeachFullSubject = dto.CanTeachFullSubject,
            Units = dto.Units,
            QuranContentTypeIds = dto.QuranContentTypeIds,
            QuranLevelIds = dto.QuranLevelIds
        }));
    }

    /// <summary>
    /// Update units / CanTeachFullSubject / Quran coverage for a specific teacher subject row.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeacherSubjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeacherSubject(int id, [FromBody] UpdateTeacherSubjectDto dto)
    {
        var command = new UpdateTeacherSubjectCommand
        {
            Id = id,
            CanTeachFullSubject = dto.CanTeachFullSubject,
            Units = dto.Units,
            QuranContentTypeIds = dto.QuranContentTypeIds,
            QuranLevelIds = dto.QuranLevelIds
        };

        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a specific teacher subject
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeacherSubject(int id)
    {
        return NewResult(await Mediator.Send(new DeleteTeacherSubjectCommand { Id = id }));
    }
}
