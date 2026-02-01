using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Commands.SaveTeacherSubjects;
using Qalam.Core.Features.Teacher.Queries.GetTeacherSubjects;
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
    /// <returns>List of subjects with their units and Quran specialization</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TeacherSubjectsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherSubjects()
    {
        // UserId is auto-populated by UserIdentityBehavior from JWT token
        return NewResult(await Mediator.Send(new GetTeacherSubjectsQuery()));
    }

    /// <summary>
    /// Save teacher subjects with units (batch operation - replaces existing)
    /// </summary>
    /// <remarks>
    /// This endpoint saves all subjects at once. Any existing subjects will be replaced.
    /// 
    /// For Quran domain:
    /// - QuranContentTypeId: 1=حفظ (Memorization), 2=تلاوة (Recitation), 3=تجويد (Tajweed)
    /// - QuranLevelId: 1=نوراني (Noorani), 2=مبتدئ (Beginner), 3=متوسط (Intermediate), 4=متقدم (Advanced)
    /// - null means "all types" or "all levels"
    /// 
    /// Example for Quran teacher:
    /// ```json
    /// {
    ///   "subjects": [
    ///     {
    ///       "subjectId": 499,
    ///       "canTeachFullSubject": false,
    ///       "units": [
    ///         { "unitId": 115, "quranContentTypeId": 1, "quranLevelId": null },
    ///         { "unitId": 116, "quranContentTypeId": 1, "quranLevelId": 2 }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="dto">Subjects with units to save</param>
    /// <returns>Saved subjects with full details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherSubjectsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveTeacherSubjects([FromBody] SaveTeacherSubjectsDto dto)
    {
        // UserId is auto-populated by UserIdentityBehavior from JWT token
        var command = new SaveTeacherSubjectsCommand
        {
            Subjects = dto.Subjects
        };

        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a specific teacher subject
    /// </summary>
    /// <param name="id">Teacher subject ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeacherSubject(int id)
    {
        // TODO: Implement delete single subject command
        return Ok(new { message = "Not implemented yet" });
    }
}
