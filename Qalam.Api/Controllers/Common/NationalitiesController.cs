using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Common.Queries.GetActiveNationalities;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Common;

namespace Qalam.Api.Controllers.Common;

/// <summary>
/// Public active nationalities list (teacher registration picker, etc.).
/// </summary>
[ApiController]
[Route(Router.Nationalities)]
[AllowAnonymous]
[Tags("Common · Nationalities")]
public class NationalitiesController : AppControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<NationalityPublicDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive()
    {
        return NewResult(await Mediator.Send(new GetActiveNationalitiesQuery()));
    }
}
