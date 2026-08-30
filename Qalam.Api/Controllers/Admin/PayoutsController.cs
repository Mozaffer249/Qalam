using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Finance.Commands.ApproveAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.CancelAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.CreateAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.MarkAdminPayoutBatchFailed;
using Qalam.Core.Features.Admin.Finance.Commands.MarkAdminPayoutBatchPaid;
using Qalam.Core.Features.Admin.Finance.Commands.ProcessAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.RejectAdminPayoutBatch;
using Qalam.Core.Features.Admin.Finance.Commands.RetryAdminPayoutBatch;
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
    private int? CurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var uid) ? uid : null;
    }

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
        => NewResult(await Mediator.Send(new CreateAdminPayoutBatchCommand
        {
            Body = body,
            CreatedByUserId = CurrentUserId()
        }, cancellationToken));

    [HttpPost(Router.AdminPayoutApprove)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ApproveAdminPayoutBatchCommand
        {
            Id = id,
            ApprovedByUserId = CurrentUserId()
        }, cancellationToken));

    [HttpPost(Router.AdminPayoutReject)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] PayoutActionReasonDto? body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new RejectAdminPayoutBatchCommand
        {
            Id = id,
            Reason = body?.Reason
        }, cancellationToken));

    [HttpPost(Router.AdminPayoutCancel)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] PayoutActionReasonDto? body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new CancelAdminPayoutBatchCommand
        {
            Id = id,
            Reason = body?.Reason
        }, cancellationToken));

    [HttpPost(Router.AdminPayoutProcess)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Process(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ProcessAdminPayoutBatchCommand
        {
            Id = id,
            ProcessedByUserId = CurrentUserId()
        }, cancellationToken));

    [HttpPost(Router.AdminPayoutMarkPaid)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new MarkAdminPayoutBatchPaidCommand { Id = id }, cancellationToken));

    [HttpPost(Router.AdminPayoutFail)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkFailed(
        int id,
        [FromBody] PayoutActionReasonDto? body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new MarkAdminPayoutBatchFailedCommand
        {
            Id = id,
            Reason = body?.Reason
        }, cancellationToken));

    [HttpPost(Router.AdminPayoutRetry)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Retry(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new RetryAdminPayoutBatchCommand
        {
            Id = id,
            ProcessedByUserId = CurrentUserId()
        }, cancellationToken));
}
