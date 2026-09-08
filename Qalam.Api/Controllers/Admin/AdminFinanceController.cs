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
using Qalam.Core.Features.Admin.Payments.Commands.StartAdminPaymentReconciliation;
using Qalam.Core.Features.Admin.Payments.Queries.GetAdminPaymentReconciliationRun;
using Qalam.Core.Features.Admin.Payments.Queries.ListAdminPaymentEvents;
using Qalam.Core.Features.Admin.Payments.Queries.ListAdminPaymentEventsByPayment;
using Qalam.Core.Features.Admin.Payments.Queries.ListAdminPaymentReconciliationRuns;
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

    [HttpGet(Router.AdminPaymentEvents)]
    [ProducesResponseType(typeof(PagedResult<AdminPaymentTransactionEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPaymentEvents(
        [FromQuery] AdminPaymentTransactionEventFilter filter,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminPaymentEventsQuery { Filter = filter }, cancellationToken));

    [HttpGet(Router.AdminPaymentEventsByPayment)]
    [ProducesResponseType(typeof(IReadOnlyList<AdminPaymentTransactionEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPaymentEventsByPayment(
        int paymentId,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminPaymentEventsByPaymentQuery { PaymentId = paymentId }, cancellationToken));

    [HttpGet(Router.AdminPaymentReconciliationRuns)]
    [ProducesResponseType(typeof(PagedResult<AdminPaymentReconciliationRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReconciliationRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminPaymentReconciliationRunsQuery
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet(Router.AdminPaymentReconciliationRunById)]
    [ProducesResponseType(typeof(AdminPaymentReconciliationRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReconciliationRun(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminPaymentReconciliationRunQuery { Id = id }, cancellationToken));

    [HttpPost(Router.AdminPaymentReconciliationStart)]
    [ProducesResponseType(typeof(AdminPaymentReconciliationRunDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartReconciliation(
        [FromBody] StartPaymentReconciliationRequestDto body,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _ = int.TryParse(userIdClaim, out var userId);
        return NewResult(await Mediator.Send(new StartAdminPaymentReconciliationCommand
        {
            UserId = userId,
            Data = body ?? new StartPaymentReconciliationRequestDto()
        }, cancellationToken));
    }
}
