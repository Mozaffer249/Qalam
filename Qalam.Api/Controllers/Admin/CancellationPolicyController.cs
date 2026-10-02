using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.CancellationPolicy;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Policy;

namespace Qalam.Api.Controllers.Admin;

/// <summary>Versioned cancellation &amp; refund policy. Reads for admins; changes for SuperAdmin.</summary>
[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Cancellation & Refund Policy")]
public class CancellationPolicyController : AppControllerBase
{
    [HttpGet(Router.AdminCancellationPolicyCurrent)]
    [ProducesResponseType(typeof(PolicyVersionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetCurrentCancellationPolicyQuery(), cancellationToken));

    [HttpGet(Router.AdminCancellationPolicyVersions)]
    [ProducesResponseType(typeof(List<PolicyVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVersions(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListCancellationPolicyVersionsQuery(), cancellationToken));

    [HttpGet(Router.AdminCancellationPolicyVersionById)]
    [ProducesResponseType(typeof(PolicyVersionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersion([FromRoute] int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetCancellationPolicyVersionQuery { Id = id }, cancellationToken));

    [HttpGet(Router.AdminCancellationPolicyCompare)]
    [ProducesResponseType(typeof(PolicyCompareDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare([FromQuery] int fromId, [FromQuery] int toId, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new CompareCancellationPolicyVersionsQuery { FromId = fromId, ToId = toId }, cancellationToken));

    [HttpGet(Router.AdminCancellationPolicyDraft)]
    [ProducesResponseType(typeof(PolicyVersionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDraft(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetCancellationPolicyDraftQuery(), cancellationToken));

    [HttpPut(Router.AdminCancellationPolicyDraft)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PolicyVersionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveDraft([FromBody] SavePolicyDraftDto body, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new SaveCancellationPolicyDraftCommand
        {
            UserId = CurrentUserId(),
            Data = body ?? new SavePolicyDraftDto()
        }, cancellationToken));

    [HttpPost(Router.AdminCancellationPolicyDraftPublish)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PolicyVersionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish([FromBody] PublishPolicyDraftDto? body, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new PublishCancellationPolicyDraftCommand
        {
            UserId = CurrentUserId(),
            Data = body ?? new PublishPolicyDraftDto()
        }, cancellationToken));

    [HttpDelete(Router.AdminCancellationPolicyDraft)]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> DiscardDraft(CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new DiscardCancellationPolicyDraftCommand(), cancellationToken));

    private int? CurrentUserId()
    {
        var claim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
