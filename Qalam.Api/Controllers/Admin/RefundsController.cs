using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Finance.Commands.IssueAdminRefund;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminRefundById;
using Qalam.Core.Features.Admin.Finance.Queries.ListAdminRefunds;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using System.Security.Claims;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Refunds")]
public class RefundsController : AppControllerBase
{
    [HttpGet(Router.AdminRefunds)]
    [ProducesResponseType(typeof(PagedResult<AdminRefundListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] RefundStatus? status = null,
        [FromQuery] int? enrollmentId = null,
        [FromQuery] int? teacherId = null,
        [FromQuery] int? studentId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminRefundsQuery
        {
            Status = status,
            EnrollmentId = enrollmentId,
            TeacherId = teacherId,
            StudentId = studentId,
            Search = search,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet(Router.AdminRefundById)]
    [ProducesResponseType(typeof(AdminRefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminRefundByIdQuery { Id = id }, cancellationToken));

    [HttpPost(Router.AdminRefunds)]
    [ProducesResponseType(typeof(AdminRefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Issue(
        [FromBody] IssueAdminRefundDto body,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        return NewResult(await Mediator.Send(new IssueAdminRefundCommand
        {
            Body = body,
            InitiatedByUserId = userId
        }, cancellationToken));
    }
}
