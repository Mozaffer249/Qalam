using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Core.Features.Admin.Lrs.Commands;
using Qalam.Core.Features.Admin.Lrs.Queries;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Messaging;
using Qalam.Service.Abstracts;

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

    /// <summary>
    /// Diagnostic: publishes a single test message straight to the LRS RabbitMQ queue and
    /// reports whether the broker confirmed it. Isolates the publish path from the admin
    /// dispatch flow. With publisher confirms enabled, a broker-side failure surfaces here
    /// instead of being silently swallowed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost(Router.AdminLrsTestPublish)]
    public async Task<IActionResult> TestPublish([FromServices] IRabbitMQService rabbitMQ)
    {
        var testMessage = new LrsStatementMessage
        {
            StatementJson = $"{{\"diagnostic\":true,\"ts\":\"{DateTime.UtcNow:o}\"}}",
            Verb = "diagnostic",
            ActorName = "diagnostic-test",
            ObjectId = "diagnostic/test",
            Timestamp = DateTime.UtcNow,
            SourceEntityType = "Diagnostic",
            SourceEntityId = 0,
            QueuedAt = DateTime.UtcNow
        };

        try
        {
            await rabbitMQ.QueueLrsStatementAsync(testMessage);
            return Ok(new
            {
                success = true,
                message = "Broker confirmed the test publish. With the consumer stopped, lrs-queue depth should now be 1."
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
                detail = ex.ToString()
            });
        }
    }
}
