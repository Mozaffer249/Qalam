using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Notifications.Commands.MarkAllTeacherNotificationsRead;
using Qalam.Core.Features.Teacher.Notifications.Commands.MarkTeacherNotificationRead;
using Qalam.Core.Features.Teacher.Notifications.Queries.GetTeacherNotifications;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherNotifications)]
public class TeacherNotificationsController : AppControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(TeacherNotificationsPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] GetTeacherNotificationsQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id)
        => NewResult(await Mediator.Send(new MarkTeacherNotificationReadCommand { Id = id }));

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead()
        => NewResult(await Mediator.Send(new MarkAllTeacherNotificationsReadCommand()));
}
