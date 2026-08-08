using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Commands.CloseContactMessage;
using Qalam.Core.Features.Admin.Commands.ReopenContactMessage;
using Qalam.Core.Features.Admin.Commands.SetContactMessageInProgress;
using Qalam.Core.Features.Admin.Queries.GetContactMessageById;
using Qalam.Core.Features.Admin.Queries.GetContactMessagesList;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Contact")]
public class ContactMessagesController : AppControllerBase
{
    [HttpGet(Router.AdminContactMessages)]
    [ProducesResponseType(typeof(List<AdminContactMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? reason = null,
        [FromQuery] string? status = null)
    {
        return NewResult(await Mediator.Send(new GetContactMessagesListQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            Reason = reason,
            Status = status
        }));
    }

    [HttpGet(Router.AdminContactMessageById)]
    [ProducesResponseType(typeof(AdminContactMessageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        return NewResult(await Mediator.Send(new GetContactMessageByIdQuery { Id = id }));
    }

    [HttpPost(Router.AdminContactMessageClose)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(
        [FromRoute] int id,
        [FromBody] CloseContactMessageRequest? body)
    {
        return NewResult(await Mediator.Send(new CloseContactMessageCommand
        {
            Id = id,
            AdminNote = body?.AdminNote
        }));
    }

    [HttpPost(Router.AdminContactMessageReopen)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reopen([FromRoute] int id)
    {
        return NewResult(await Mediator.Send(new ReopenContactMessageCommand { Id = id }));
    }

    [HttpPost(Router.AdminContactMessageInProgress)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetInProgress([FromRoute] int id)
    {
        return NewResult(await Mediator.Send(new SetContactMessageInProgressCommand { Id = id }));
    }
}
