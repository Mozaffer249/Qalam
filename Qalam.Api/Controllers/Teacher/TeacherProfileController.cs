using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route("Api/V1/Teacher/Profile")]
public class TeacherProfileController : AppControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(TeacherMyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
        => NewResult(await Mediator.Send(new GetMyTeacherProfileQuery()));
}
