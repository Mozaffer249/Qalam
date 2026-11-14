using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Core.Features.Admin.Lrs.Commands;
using Qalam.Core.Features.Admin.Lrs.Queries;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.context;
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

    /// <summary>
    /// Diagnostic: builds an xAPI statement and POSTs it DIRECTLY to the NELC LRS endpoint
    /// (Basic auth from DB credentials), bypassing RabbitMQ and the consumer entirely.
    /// Returns NELC's HTTP status and response body so the send path can be tested in isolation.
    /// </summary>
    [AllowAnonymous]
    [HttpPost(Router.AdminLrsTestSendDirect)]
    public async Task<IActionResult> TestSendDirect(
        [FromServices] ApplicationDBContext db,
        [FromQuery] string nationalId = "1066181346",
        [FromQuery] string courseId = "6009",
        [FromQuery] string verb = "attended",
        [FromQuery] string? email = null)
    {
        var row = await db.Set<SystemSetting>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "LrsCredentials");
        if (row == null || string.IsNullOrEmpty(row.Value))
            return Ok(new { success = false, error = "LrsCredentials not configured in DB." });

        var creds = JsonSerializer.Deserialize<JsonElement>(row.Value);
        var endpoint = creds.TryGetProperty("endpoint", out var ep) ? ep.GetString() : null;
        var key = creds.TryGetProperty("key", out var k) ? k.GetString() : null;
        var secret = creds.TryGetProperty("secret", out var s) ? s.GetString() : null;
        var version = creds.TryGetProperty("xApiVersion", out var ver) && !string.IsNullOrEmpty(ver.GetString())
            ? ver.GetString()! : "1.0.3";
        var platform = creds.TryGetProperty("platformKey", out var pk) && !string.IsNullOrEmpty(pk.GetString())
            ? pk.GetString()!
            : (creds.TryGetProperty("platformIri", out var pi) ? pi.GetString() ?? "https://qalam.net.sa" : "https://qalam.net.sa");

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
            return Ok(new { success = false, error = "LrsCredentials incomplete (endpoint/key/secret).", endpoint, key });

        // xAPI allows exactly one inverse-functional identifier: mbox (email) when provided, else account.
        object actor = string.IsNullOrEmpty(email)
            ? new
            {
                objectType = "Agent",
                name = nationalId,
                account = new { homePage = "https://qalam.net.sa", name = nationalId }
            }
            : new
            {
                objectType = "Agent",
                name = nationalId,
                mbox = $"mailto:{email}"
            };

        var statement = new
        {
            actor,
            verb = new
            {
                id = $"http://adlnet.gov/expapi/verbs/{verb}",
                display = new Dictionary<string, string> { ["en-US"] = verb }
            },
            @object = new
            {
                objectType = "Activity",
                id = $"https://qalam.net.sa/course/{courseId}",
                definition = new
                {
                    type = "https://w3id.org/xapi/cmi5/activitytype/course",
                    name = new Dictionary<string, string> { ["ar-SA"] = "diagnostic-direct" }
                }
            },
            result = new { completion = true, success = true },
            context = new { platform, language = "ar-SA" },
            timestamp = DateTime.UtcNow.ToString("o")
        };
        var json = JsonSerializer.Serialize(statement);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var authBytes = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authBytes);
        client.DefaultRequestHeaders.Add("X-Experience-API-Version", version);

        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            return Ok(new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                endpoint,
                nelcResponse = responseBody,
                sentStatement = json
            });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, error = ex.Message, detail = ex.ToString(), endpoint });
        }
    }

    /// <summary>
    /// Public, reachable certificate page for a completed enrollment. The <c>earned</c> xAPI statement's
    /// jws-certificate-location points here, and NELC fetches it (CRT002 requires a 200, not a 404).
    /// </summary>
    [AllowAnonymous]
    [HttpGet(Router.AdminLrsCertificate)]
    public async Task<IActionResult> Certificate([FromServices] ApplicationDBContext db, [FromRoute] int enrollmentId)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Course).ThenInclude(c => c.Teacher).ThenInclude(t => t.User)
            .Include(e => e.Course).ThenInclude(c => c.TeacherSubject).ThenInclude(ts => ts.Subject).ThenInclude(s => s.Domain)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment?.Course == null)
            return NotFound();

        var learner = enrollment.Participants
            .Select(p => p.Student?.User)
            .FirstOrDefault(u => u != null);
        var teacher = enrollment.Course.Teacher?.User;
        var domain = enrollment.Course.Domain;

        var verificationUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";

        var model = new Qalam.Service.Implementations.LRS.Building.CertificateModel
        {
            LearnerName = learner != null ? $"{learner.FirstName} {learner.LastName}".Trim() : "—",
            CourseTitle = enrollment.Course.Title,
            InstructorName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}".Trim() : null,
            DomainNameAr = domain?.NameAr,
            DomainNameEn = domain?.NameEn,
            IssuedOn = enrollment.CompletedAt ?? enrollment.ActivatedAt ?? DateTime.UtcNow,
            CertificateId = enrollment.Id,
            VerificationUrl = verificationUrl,
        };

        var html = Qalam.Service.Implementations.LRS.Building.CertificateHtmlBuilder.Build(model);
        return Content(html, "text/html; charset=utf-8");
    }
}
