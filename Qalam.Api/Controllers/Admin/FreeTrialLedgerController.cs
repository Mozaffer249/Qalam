using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Free trial ledger")]
public class FreeTrialLedgerController : AppControllerBase
{
    private readonly IFreeSessionLedgerReadService _ledgerReadService;

    public FreeTrialLedgerController(IFreeSessionLedgerReadService ledgerReadService)
    {
        _ledgerReadService = ledgerReadService;
    }

    [HttpGet(Router.AdminStudentFreeTrialConsumptions)]
    [ProducesResponseType(typeof(List<AdminStudentFreeTrialConsumptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListStudentConsumptions(
        [FromRoute] int studentId,
        CancellationToken cancellationToken = default)
    {
        var items = await _ledgerReadService.ListStudentConsumptionsAsync(studentId, cancellationToken);
        return Ok(items);
    }

    [HttpGet(Router.AdminTeacherInterviewUnlocks)]
    [ProducesResponseType(typeof(List<AdminTeacherInterviewUnlockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTeacherInterviewUnlocks(
        [FromRoute] int teacherId,
        CancellationToken cancellationToken = default)
    {
        var items = await _ledgerReadService.ListTeacherInterviewUnlocksAsync(teacherId, cancellationToken);
        return Ok(items);
    }
}
