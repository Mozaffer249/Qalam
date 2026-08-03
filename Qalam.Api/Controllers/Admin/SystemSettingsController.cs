using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Commands.UpdateAuthSettings;
using Qalam.Core.Features.Admin.Commands.UpdateTeacherAccessSettings;
using Qalam.Core.Features.Admin.Queries.GetAuthSettings;
using Qalam.Core.Features.Admin.Queries.GetTeacherAccessSettings;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Auth;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Route("Api/V1/Admin/[controller]")]
[Authorize(Roles = Roles.SuperAdmin)]
[Tags("Admin · System Settings")]
public class SystemSettingsController : AppControllerBase
{
    /// <summary>
    /// Get auth settings (admin JSON).
    /// </summary>
    [HttpGet("Auth")]
    [ProducesResponseType(typeof(AuthSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthSettings()
    {
        return NewResult(await Mediator.Send(new GetAuthSettingsQuery()));
    }

    /// <summary>
    /// Update auth settings (admin JSON).
    /// </summary>
    [HttpPut("Auth")]
    [ProducesResponseType(typeof(AuthSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAuthSettings([FromBody] AuthSettingsDto settings)
    {
        return NewResult(await Mediator.Send(new UpdateAuthSettingsCommand { Settings = settings }));
    }

    /// <summary>
    /// Get teacher dashboard launch gate settings.
    /// </summary>
    [HttpGet("TeacherAccess")]
    [ProducesResponseType(typeof(TeacherAccessSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeacherAccessSettings()
    {
        return NewResult(await Mediator.Send(new GetTeacherAccessSettingsQuery()));
    }

    /// <summary>
    /// Update teacher dashboard launch gate. Set <c>teacherDashboardReady: true</c> when the platform is ready to publish.
    /// </summary>
    [HttpPut("TeacherAccess")]
    [ProducesResponseType(typeof(TeacherAccessSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTeacherAccessSettings([FromBody] TeacherAccessSettingsDto settings)
    {
        return NewResult(await Mediator.Send(new UpdateTeacherAccessSettingsCommand { Settings = settings }));
    }
}
