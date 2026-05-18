using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Commands.UpdateAuthSettings;
using Qalam.Core.Features.Admin.Queries.GetAuthSettings;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Auth;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Route("Api/V1/Admin/[controller]")]
[Authorize(Roles = Roles.SuperAdmin)]
[Tags("Admin · Auth Settings")]
public class SystemSettingsController : AppControllerBase
{
    /// <summary>
    /// Get auth settings (admin JSON).
    /// </summary>
    /// <remarks>
    /// SuperAdmin only. Same database row as public Config (`Auth.Settings`), admin DTO with `registerRequiresEmail` / `registerRequiresPhone`.
    /// See `docs/Auth-Config-Frontend.md` (Admin section).
    /// </remarks>
    [HttpGet("Auth")]
    [ProducesResponseType(typeof(AuthSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthSettings()
    {
        return NewResult(await Mediator.Send(new GetAuthSettingsQuery()));
    }

    /// <summary>
    /// Update auth settings (admin JSON).
    /// </summary>
    /// <remarks>
    /// SuperAdmin only. PUT full body: `teacher`, `student`, `otp`. Use `otpDelivery: "Sms"` only if SMS is enabled on the server.
    /// See `docs/Auth-Config-Frontend.md` (Admin section).
    /// </remarks>
    [HttpPut("Auth")]
    [ProducesResponseType(typeof(AuthSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAuthSettings([FromBody] AuthSettingsDto settings)
    {
        return NewResult(await Mediator.Send(new UpdateAuthSettingsCommand { Settings = settings }));
    }
}
