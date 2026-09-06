using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Authentication.Commands.RegisterDeviceToken;
using Qalam.Core.Features.Authentication.Commands.UnregisterDeviceToken;
using Qalam.Core.Features.Authentication.Commands.UpdateNotificationPreferences;
using Qalam.Core.Features.Authentication.Queries.GetNotificationPreferences;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Account;

namespace Qalam.Api.Controllers.Authentication.Core;

/// <summary>Authenticated account notification preferences and device tokens.</summary>
[ApiController]
[Authorize]
[Tags("Account · Notifications")]
public class AccountNotificationsController : AppControllerBase
{
    [HttpGet(Router.AccountNotificationPreferences)]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences()
        => NewResult(await Mediator.Send(new GetNotificationPreferencesQuery()));

    [HttpPut(Router.AccountNotificationPreferences)]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpPost(Router.AccountDeviceTokens)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpDelete(Router.AccountDeviceTokens)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterDeviceToken([FromBody] UnregisterDeviceTokenCommand command)
        => NewResult(await Mediator.Send(command));
}
