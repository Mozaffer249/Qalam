using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;
using System.Net;
using System.Security.Claims;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Payouts")]
public class PayoutsController : AppControllerBase
{
    private readonly IPayoutService _payoutService;

    public PayoutsController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    [HttpGet(Router.AdminPayoutPendingEarnings)]
    [ProducesResponseType(typeof(List<AdminPendingEarningDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPendingEarnings(CancellationToken cancellationToken = default)
        => NewResult(OkResponse(await _payoutService.ListPendingEarningsAsync(cancellationToken)));

    [HttpGet(Router.AdminPayouts)]
    [ProducesResponseType(typeof(List<AdminPayoutBatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBatches(CancellationToken cancellationToken = default)
        => NewResult(OkResponse(await _payoutService.ListBatchesAsync(cancellationToken)));

    [HttpGet(Router.AdminPayoutById)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(int id, CancellationToken cancellationToken = default)
    {
        var batch = await _payoutService.GetBatchAsync(id, cancellationToken);
        if (batch == null)
            return NewResult(FailResponse<AdminPayoutBatchDto>("Payout batch not found.", HttpStatusCode.NotFound));
        return NewResult(OkResponse(batch));
    }

    [HttpPost(Router.AdminPayouts)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreatePayoutBatchDto? body,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;
        try
        {
            var batch = await _payoutService.CreateBatchFromPendingAsync(
                body?.PeriodStart,
                body?.PeriodEnd,
                userId,
                cancellationToken);
            return NewResult(OkResponse(batch));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(FailResponse<AdminPayoutBatchDto>(ex.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost(Router.AdminPayoutApprove)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = await _payoutService.ApproveAsync(id, cancellationToken);
            if (batch == null)
                return NewResult(FailResponse<AdminPayoutBatchDto>("Payout batch not found.", HttpStatusCode.NotFound));
            return NewResult(OkResponse(batch));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(FailResponse<AdminPayoutBatchDto>(ex.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost(Router.AdminPayoutMarkPaid)]
    [ProducesResponseType(typeof(AdminPayoutBatchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = await _payoutService.MarkPaidAsync(id, cancellationToken);
            if (batch == null)
                return NewResult(FailResponse<AdminPayoutBatchDto>("Payout batch not found.", HttpStatusCode.NotFound));
            return NewResult(OkResponse(batch));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(FailResponse<AdminPayoutBatchDto>(ex.Message, HttpStatusCode.BadRequest));
        }
    }

    private static Response<T> OkResponse<T>(T data) => new(data)
    {
        StatusCode = HttpStatusCode.OK,
        Succeeded = true,
        Message = "Success"
    };

    private static Response<T> FailResponse<T>(string message, HttpStatusCode code) => new(message)
    {
        StatusCode = code,
        Succeeded = false
    };
}
