using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Common;

/// <summary>
/// LiveKit server webhooks (join / leave). Authenticated via LiveKit JWT, not user Bearer tokens.
/// See https://docs.livekit.io/intro/basics/rooms-participants-tracks/webhooks-events/
/// </summary>
[ApiController]
[AllowAnonymous]
public class LiveKitWebhooksController : ControllerBase
{
    private readonly ILivePresenceWebhookService _presenceWebhookService;
    private readonly ILogger<LiveKitWebhooksController> _logger;

    public LiveKitWebhooksController(
        ILivePresenceWebhookService presenceWebhookService,
        ILogger<LiveKitWebhooksController> logger)
    {
        _presenceWebhookService = presenceWebhookService;
        _logger = logger;
    }

    /// <summary>LiveKit Cloud / self-hosted webhook receiver: Api/V1/Live/Webhooks/LiveKit</summary>
    [HttpPost(Router.LiveKitWebhook)]
    [Consumes("application/webhook+json", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // LiveKit requires the raw body string for JWT payload hash verification.
        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var auth = Request.Headers.Authorization.ToString();
        var hasAuth = !string.IsNullOrWhiteSpace(auth);
        var contentType = Request.ContentType ?? "(none)";

        _logger.LogInformation(
            "LiveKit webhook received: bodyLength={BodyLength}, hasAuthorization={HasAuth}, contentType={ContentType}",
            rawBody?.Length ?? 0,
            hasAuth,
            contentType);

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            _logger.LogWarning(
                "LiveKit webhook empty body (contentType={ContentType}). Ensure the raw POST body is not consumed before this endpoint.",
                contentType);
        }

        var (ok, statusCode, message) = await _presenceWebhookService.HandleLiveKitAsync(
            rawBody,
            hasAuth ? auth : null,
            cancellationToken);

        _logger.LogInformation(
            "LiveKit webhook outcome: ok={Ok}, statusCode={StatusCode}, message={Message}",
            ok,
            statusCode,
            message);

        if (!ok)
            return StatusCode(statusCode, new { message });

        return Ok(new { message });
    }
}
