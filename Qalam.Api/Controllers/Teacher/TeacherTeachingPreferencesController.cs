using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.TeachingPreferences.Commands.UpdateTeacherTeachingPreferences;
using Qalam.Core.Features.Teacher.TeachingPreferences.Queries.GetTeacherTeachingPreferences;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherTeachingPreferencesController : AppControllerBase
{
    [HttpGet(Router.TeacherTeachingPreferences)]
    [ProducesResponseType(typeof(TeacherTeachingPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeachingPreferences()
        => NewResult(await Mediator.Send(new GetTeacherTeachingPreferencesQuery()));

    [HttpPut(Router.TeacherTeachingPreferences)]
    [ProducesResponseType(typeof(TeacherTeachingPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeachingPreferences([FromBody] UpdateTeacherTeachingPreferencesDto dto)
    {
        var command = new UpdateTeacherTeachingPreferencesCommand
        {
            OffersOnline = dto.OffersOnline,
            OffersInPerson = dto.OffersInPerson,
            OffersIndividual = dto.OffersIndividual,
            OffersGroup = dto.OffersGroup,
            JobTitle = dto.JobTitle,
            YearsOfExperience = dto.YearsOfExperience
        };

        return NewResult(await Mediator.Send(command));
    }
}
