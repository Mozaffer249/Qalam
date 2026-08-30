using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminFinanceSummary;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminFinanceTransactionByKey;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminRevenueById;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminRevenueSummary;
using Qalam.Core.Features.Admin.Finance.Queries.GetAdminTeacherFinanceSummary;
using Qalam.Core.Features.Admin.Finance.Queries.ListAdminFinanceTransactions;
using Qalam.Core.Features.Admin.Finance.Queries.ListAdminRevenueRecords;
using Qalam.Core.Features.Admin.Finance.Queries.ListAdminTeacherFinanceTransactions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Finance")]
public class AdminFinanceController : AppControllerBase
{
    [HttpGet(Router.AdminFinanceSummary)]
    [ProducesResponseType(typeof(AdminFinanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminFinanceSummaryQuery
        {
            FromUtc = fromUtc,
            ToUtc = toUtc
        }, cancellationToken));

    [HttpGet(Router.AdminFinanceTransactions)]
    [ProducesResponseType(typeof(PagedResult<AdminFinanceTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTransactions(
        [FromQuery] AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminFinanceTransactionsQuery
        {
            Filter = filter
        }, cancellationToken));

    [HttpGet(Router.AdminFinanceTransactionByKey)]
    [ProducesResponseType(typeof(TeacherFinanceTransactionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(
        string key,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminFinanceTransactionByKeyQuery
        {
            Key = key
        }, cancellationToken));

    [HttpGet(Router.AdminTeacherFinanceSummary)]
    [ProducesResponseType(typeof(AdminTeacherFinanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TeacherSummary(
        int teacherId,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminTeacherFinanceSummaryQuery
        {
            TeacherId = teacherId
        }, cancellationToken));

    [HttpGet(Router.AdminTeacherFinanceTransactions)]
    [ProducesResponseType(typeof(PagedResult<AdminFinanceTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TeacherTransactions(
        int teacherId,
        [FromQuery] AdminFinanceTransactionFilter filter,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminTeacherFinanceTransactionsQuery
        {
            TeacherId = teacherId,
            Filter = filter
        }, cancellationToken));

    [HttpGet(Router.AdminRevenueSummary)]
    [ProducesResponseType(typeof(AdminRevenueSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevenueSummary(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminRevenueSummaryQuery
        {
            FromUtc = fromUtc,
            ToUtc = toUtc
        }, cancellationToken));

    [HttpGet(Router.AdminRevenue)]
    [ProducesResponseType(typeof(PagedResult<AdminRevenueRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRevenue(
        [FromQuery] AdminRevenueListFilter filter,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminRevenueRecordsQuery
        {
            Filter = filter
        }, cancellationToken));

    [HttpGet(Router.AdminRevenueById)]
    [ProducesResponseType(typeof(AdminRevenueDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRevenue(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminRevenueByIdQuery { Id = id }, cancellationToken));
}
