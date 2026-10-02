using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.PolicyCases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Policy;

namespace Qalam.Api.Controllers.Admin;

/// <summary>Applied cancellation/refund policy cases: list, detail, admin exceptions, reversals, enrollment timeline.</summary>
[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Policy Cases")]
public class PolicyCasesController : AppControllerBase
{
    [HttpGet(Router.AdminPolicyCases)]
    [ProducesResponseType(typeof(PolicyCaseListResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PolicyCaseListFilter filter, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListPolicyCasesQuery { Filter = filter }, cancellationToken));

    [HttpGet(Router.AdminPolicyCaseById)]
    [ProducesResponseType(typeof(PolicyCaseDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetPolicyCaseQuery { Id = id }, cancellationToken));

    [HttpPost(Router.AdminPolicyCaseExceptions)]
    [ProducesResponseType(typeof(PolicyCaseDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyException([FromBody] AdminPolicyExceptionRequest body, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ApplyPolicyExceptionCommand
        {
            UserId = CurrentUserId(),
            IsSuperAdmin = User.IsInRole(Roles.SuperAdmin),
            Data = body
        }, cancellationToken));

    [HttpPost(Router.AdminPolicyCaseReverse)]
    [ProducesResponseType(typeof(PolicyCaseDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reverse([FromRoute] int id, [FromBody] ReversePolicyCaseRequest body, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ReversePolicyCaseCommand
        {
            Id = id,
            UserId = CurrentUserId(),
            Reason = body?.Reason ?? ""
        }, cancellationToken));

    [HttpGet(Router.AdminEnrollmentFinancialTimeline)]
    [ProducesResponseType(typeof(List<FinancialTimelineEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnrollmentTimeline([FromRoute] int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetEnrollmentFinancialTimelineQuery { EnrollmentId = id }, cancellationToken));

    private int? CurrentUserId()
    {
        var claim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
