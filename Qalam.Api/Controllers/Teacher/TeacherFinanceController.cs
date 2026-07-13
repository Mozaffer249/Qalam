using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceSummary;
using Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceTransactions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Results;

namespace Qalam.Api.Controllers.Teacher;

[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route("Api/V1/Teacher/Finance")]
public class TeacherFinanceController : AppControllerBase
{
    [HttpGet("Summary")]
    [ProducesResponseType(typeof(TeacherFinanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
        => NewResult(await Mediator.Send(new GetFinanceSummaryQuery()));

    [HttpGet("Transactions")]
    [ProducesResponseType(typeof(PaginatedResult<TeacherFinanceTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromQuery] GetFinanceTransactionsQuery query)
        => NewResult(await Mediator.Send(query));
}
