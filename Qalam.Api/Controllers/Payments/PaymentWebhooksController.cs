using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Payment;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Api.Controllers.Payments;

/// <summary>
/// Thin webhook receiver for all payment gateways. Auth + parse live in each IPaymentGateway;
/// domain mutation lives in IPaymentWebhookService.
/// </summary>
[ApiController]
[AllowAnonymous]
public class PaymentWebhooksController : ControllerBase
{
    private readonly IPaymentWebhookService _webhookService;

    public PaymentWebhooksController(IPaymentWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost(Router.PaymentWebhook)]
    [HttpPost(Router.MoyasarWebhook)]
    [HttpPost(Router.MoyasarWebhookSingularAlias)]
    [Consumes("application/json", "application/x-www-form-urlencoded", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Receive(string? provider, CancellationToken cancellationToken)
    {
        // Singular alias /Api/V1/Payments/Webhook has no {provider} — treat as Moyasar.
        var resolvedProvider = string.IsNullOrWhiteSpace(provider)
            ? MoyasarPaymentGateway.Name
            : provider;

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var result = await _webhookService.HandleAsync(
            resolvedProvider,
            rawBody,
            headers,
            cancellationToken);

        return result.Status switch
        {
            PaymentWebhookHandleStatus.Unauthorized => Unauthorized(new { message = result.Message }),
            PaymentWebhookHandleStatus.NotConfigured => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = result.Message }),
            _ => Ok(new { message = result.Message })
        };
    }
}
