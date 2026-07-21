using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Nationalities.Commands.CreateNationality;
using Qalam.Core.Features.Admin.Nationalities.Commands.DeleteNationality;
using Qalam.Core.Features.Admin.Nationalities.Commands.SetNationalityActive;
using Qalam.Core.Features.Admin.Nationalities.Commands.UpdateNationality;
using Qalam.Core.Features.Admin.Nationalities.Queries.GetNationalityById;
using Qalam.Core.Features.Admin.Nationalities.Queries.ListNationalities;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Common;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// SuperAdmin CRUD for the nationality / country lookup catalog.
/// </summary>
[ApiController]
[Route(Router.AdminNationalities)]
[Authorize(Roles = Roles.SuperAdmin)]
[Tags("Admin · Nationalities")]
public class NationalitiesController : AppControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<NationalityAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        return NewResult(await Mediator.Send(new ListNationalitiesQuery()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NationalityAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return NewResult(await Mediator.Send(new GetNationalityByIdQuery { Id = id }));
    }

    [HttpPost]
    [ProducesResponseType(typeof(NationalityAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNationalityDto data)
    {
        return NewResult(await Mediator.Send(new CreateNationalityCommand { Data = data }));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(NationalityAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNationalityDto data)
    {
        return NewResult(await Mediator.Send(new UpdateNationalityCommand { Id = id, Data = data }));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        return NewResult(await Mediator.Send(new DeleteNationalityCommand { Id = id }));
    }

    [HttpPatch("{id:int}/active")]
    [ProducesResponseType(typeof(NationalityAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetNationalityActiveDto data)
    {
        return NewResult(await Mediator.Send(new SetNationalityActiveCommand { Id = id, Data = data }));
    }
}
