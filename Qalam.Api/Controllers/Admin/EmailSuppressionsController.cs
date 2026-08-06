using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Commands.SeedEmailSuppressions;
using Qalam.Core.Features.Admin.Queries.GetEmailSuppressions;
using Qalam.Core.Features.Admin.Queries.GetFailedEmailContacts;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Email")]
public class EmailSuppressionsController : AppControllerBase
{
    /// <summary>
    /// Failed OTP / registration verification emails (MessageLogs Status=Failed).
    /// </summary>
    [HttpGet(Router.AdminEmailFailedContacts)]
    [ProducesResponseType(typeof(List<FailedEmailContactDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedContacts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        return NewResult(await Mediator.Send(new GetFailedEmailContactsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search
        }));
    }

    /// <summary>
    /// Addresses suppressed from outbound email (hard bounce, NDR, manual, synthetic).
    /// </summary>
    [HttpGet(Router.AdminEmailSuppressions)]
    [ProducesResponseType(typeof(List<EmailSuppressionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppressions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        return NewResult(await Mediator.Send(new GetEmailSuppressionsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search
        }));
    }

    /// <summary>
    /// Maintenance: seed suppressions from optional custom addresses and/or
    /// synthetic @phone.qalam.local accounts. Hard bounces are recorded automatically
    /// in production (SMTP 5xx / IMAP NDR). Does not delete users.
    /// </summary>
    [HttpPost(Router.AdminEmailSuppressionsSeed)]
    [ProducesResponseType(typeof(SeedEmailSuppressionsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed([FromBody] SeedEmailSuppressionsCommand? command)
    {
        command ??= new SeedEmailSuppressionsCommand();
        return NewResult(await Mediator.Send(command));
    }
}
