using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;
using System.Net;
using System.Security.Claims;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Refunds")]
public class RefundsController : AppControllerBase
{
    private readonly IRefundService _refundService;

    public RefundsController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    [HttpGet(Router.AdminRefunds)]
    [ProducesResponseType(typeof(List<AdminRefundListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] RefundStatus? status = null,
        [FromQuery] int? enrollmentId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _refundService.ListAsync(new AdminRefundListFilter
        {
            Status = status,
            EnrollmentId = enrollmentId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        }, cancellationToken);
        return NewResult(OkResponse(items));
    }

    [HttpGet(Router.AdminRefundById)]
    [ProducesResponseType(typeof(AdminRefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _refundService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NewResult(FailResponse<AdminRefundDetailDto>("Refund not found.", HttpStatusCode.NotFound));
        return NewResult(OkResponse(item));
    }

    [HttpPost(Router.AdminRefunds)]
    [ProducesResponseType(typeof(AdminRefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Issue(
        [FromBody] IssueAdminRefundDto body,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        try
        {
            if (body.PaymentId.HasValue && body.EnrollmentId.HasValue && body.Amount.HasValue)
            {
                var refund = await _refundService.IssueRefundAsync(
                    body.PaymentId.Value,
                    body.EnrollmentId.Value,
                    body.Amount.Value,
                    "SAR",
                    body.Reason,
                    userId,
                    cancellationToken);
                var detail = await _refundService.GetByIdAsync(refund.Id, cancellationToken);
                return NewResult(OkResponse(detail!));
            }

            if (body.EnrollmentId.HasValue)
            {
                var refunds = await _refundService.RefundEnrollmentPaymentsAsync(
                    body.EnrollmentId.Value,
                    string.IsNullOrWhiteSpace(body.Reason) ? "Admin refund" : body.Reason,
                    userId,
                    cancellationToken);
                if (refunds.Count == 0)
                    return NewResult(FailResponse<AdminRefundDetailDto>(
                        "No refundable payments for this enrollment.", HttpStatusCode.BadRequest));

                var detail = await _refundService.GetByIdAsync(refunds[0].Id, cancellationToken);
                return NewResult(OkResponse(detail!));
            }

            return NewResult(FailResponse<AdminRefundDetailDto>(
                "Provide PaymentId + EnrollmentId + Amount, or EnrollmentId for full refund.",
                HttpStatusCode.BadRequest));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(FailResponse<AdminRefundDetailDto>(ex.Message, HttpStatusCode.BadRequest));
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
