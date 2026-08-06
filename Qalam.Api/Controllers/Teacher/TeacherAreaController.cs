using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.TeacherAreas.Commands.AddTeacherArea;
using Qalam.Core.Features.Teacher.TeacherAreas.Commands.DeleteTeacherArea;
using Qalam.Core.Features.Teacher.TeacherAreas.Queries.GetTeacherAreas;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
public class TeacherAreaController : AppControllerBase
{
    [HttpGet(Router.TeacherArea)]
    [ProducesResponseType(typeof(List<TeacherAreaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherAreas()
        => NewResult(await Mediator.Send(new GetTeacherAreasQuery()));

    [HttpPost(Router.TeacherArea)]
    [ProducesResponseType(typeof(TeacherAreaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTeacherArea([FromBody] CreateTeacherAreaDto dto)
    {
        var command = new AddTeacherAreaCommand
        {
            LocationId = dto.LocationId,
            MaxDistanceKm = dto.MaxDistanceKm
        };

        return NewResult(await Mediator.Send(command));
    }

    [HttpDelete(Router.TeacherAreaById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeacherArea(int id)
        => NewResult(await Mediator.Send(new DeleteTeacherAreaCommand { Id = id }));
}
