using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Finance.Commands.ApproveAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.CreateAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.MarkAdminPayoutBatchPaid;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminPayoutBatchById;
using Qalam.Core.Features.Admin.Finance.Queries.ListAdminPayoutBatches;
using Qalam.Core.Features.Admin.Finance.Queries.ListAdminPendingEarnings;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using System.Security.Claims;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Payouts")]
public class PayoutsController : AppControllerBase
{
    [HttpGet(Router.AdminPayoutPendingEarnings)]
    [ProducesResponseType(typeof(PagedResult<AdminPendingEarningDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPendingEarnings(
        [FromQuery] int? teacherId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminPendingEarningsQuery
        {
            TeacherId = teacherId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet(Router.AdminPayouts)]
    [ProducesResponseType(typeof(PagedResult<AdminPayoutBatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBatches(
        [FromQuery] PayoutBatchStatus? status = null,
        [FromQuery] int? teacherId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminPayoutBatchesQuery
        {
            Status = status,
            TeacherId = teacherId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet(Router.AdminPayoutById)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminPayoutBatchByIdQuery { Id = id }, cancellationToken));

    [HttpPost(Router.AdminPayouts)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreatePayoutBatchDto? body,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        return NewResult(await Mediator.Send(new CreateAdminPayoutBatchCommand
        {
            Body = body,
            CreatedByUserId = userId
        }, cancellationToken));
    }

    [HttpPost(Router.AdminPayoutApprove)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ApproveAdminPayoutBatchCommand { Id = id }, cancellationToken));

    [HttpPost(Router.AdminPayoutMarkPaid)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new MarkAdminPayoutBatchPaidCommand { Id = id }, cancellationToken));
}
