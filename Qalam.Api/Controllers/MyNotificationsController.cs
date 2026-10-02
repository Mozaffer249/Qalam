using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Notifications;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Notifications;

namespace Qalam.Api.Controllers;

/// <summary>In-app notification inbox for the signed-in user (any role).</summary>
[ApiController]
[Authorize]
[Tags("Me · Notifications")]
public class MyNotificationsController : AppControllerBase
{
    [HttpGet(Router.MyNotifications)]
    [ProducesResponseType(typeof(UserNotificationsPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetMyNotificationsQuery
        {
            UnreadOnly = unreadOnly,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpPost(Router.MyNotificationRead)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkRead([FromRoute] int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new MarkMyNotificationReadCommand { Id = id }, cancellationToken));

    [HttpPost(Router.MyNotificationsReadAll)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new MarkAllMyNotificationsReadCommand(), cancellationToken));

    [HttpGet(Router.MyNotificationsUnreadCount)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetMyUnreadNotificationsCountQuery(), cancellationToken));
}
