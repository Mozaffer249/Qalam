using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Contact.Commands.SubmitContactMessage;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Common;

/// <summary>
/// Public contact form submissions. Optional JWT: when present, Name/Email/Phone may be omitted
/// and are prefilled from the authenticated profile.
/// </summary>
[ApiController]
[Route(Router.Contact)]
[AllowAnonymous]
[Tags("Common · Contact")]
public class ContactController : AppControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitContactMessageCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }
}
