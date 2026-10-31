using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Core.Features.Admin.Lrs.Commands;
using Qalam.Core.Features.Admin.Lrs.Queries;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · LRS")]
public class AdminLrsController : AppControllerBase
{
    [HttpGet(Router.AdminLrsStatements)]
    public async Task<IActionResult> ListStatements([FromQuery] ListLrsStatementsQuery query)
    {
        var result = await Mediator.Send(query);
        return NewResult(result);
    }

    [HttpGet(Router.AdminLrsStatementById)]
    public async Task<IActionResult> GetStatement(int id)
    {
        var result = await Mediator.Send(new GetLrsStatementByIdQuery { Id = id });
        return NewResult(result);
    }

    [HttpPost(Router.AdminLrsStatementRetry)]
    public async Task<IActionResult> RetryStatement(int id)
    {
        var result = await Mediator.Send(new RetryLrsStatementCommand { Id = id });
        return NewResult(result);
    }

    [HttpGet(Router.AdminLrsDashboard)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await Mediator.Send(new GetLrsDashboardQuery());
        return NewResult(result);
    }

    [HttpPost(Router.AdminLrsDispatchEnrollment)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DispatchEnrollment(int enrollmentId)
    {
        var result = await Mediator.Send(new DispatchEnrollmentLrsCommand { EnrollmentId = enrollmentId });
        return NewResult(result);
    }

    [HttpGet(Router.AdminLrsSettings)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Response<AdminLrsSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        return NewResult(await Mediator.Send(new GetLrsSettingsQuery()));
    }

    [HttpPut(Router.AdminLrsSettings)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Response<AdminLrsSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] AdminLrsSettingsDto settings)
    {
        return NewResult(await Mediator.Send(new UpdateLrsSettingsCommand { Settings = settings }));
    }
}
