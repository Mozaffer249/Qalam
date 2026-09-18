using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Qalam.Data.AppMetaData;
using Qalam.Data.Helpers;
using Qalam.Service.Payments;

namespace Qalam.Api.Controllers.Payments;

/// <summary>
/// Browser return landing page for hosted payment gateways.
/// Flutter WebViews intercept this URL prefix and call Confirm.
/// On web (same-tab), redirects to the student app <c>/payments/return</c> route.
/// On web (legacy popup), posts a message to the opener and tries to close itself.
/// Only signals success when Moyasar appended a payment <c>id</c> query param.
/// </summary>
[ApiController]
[AllowAnonymous]
public class PaymentReturnController : ControllerBase
{
    private readonly PaymentSettings _paymentSettings;
    private readonly IConfiguration _configuration;

    public PaymentReturnController(
        IOptions<PaymentSettings> paymentSettings,
        IConfiguration configuration)
    {
        _paymentSettings = paymentSettings.Value;
        _configuration = configuration;
    }

    [HttpGet(Router.PaymentReturn)]
    [Produces("text/html")]
    public ContentResult Return(string provider)
    {
        var safeProvider = string.IsNullOrWhiteSpace(provider) ? "payment" : provider.Trim();
        var encoded = WebUtility.HtmlEncode(safeProvider);

        var id = (Request.Query["id"].FirstOrDefault()
                  ?? Request.Query["payment_id"].FirstOrDefault()
                  ?? string.Empty).Trim();
        var ok = !string.IsNullOrEmpty(id);

        var allowedOrigins = PaymentAppReturnUrlHelper.ReadAllowedOrigins(_configuration);
        var appTarget = PaymentAppReturnUrlHelper.Resolve(
            Request.Query["app"].FirstOrDefault(),
            _paymentSettings.Moyasar.AppReturnUrl,
            allowedOrigins);

        string? redirectJs = null;
        if (!string.IsNullOrWhiteSpace(appTarget))
        {
            var sep = appTarget.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var dest = appTarget
                + sep
                + "provider=" + Uri.EscapeDataString(safeProvider)
                + "&ok=" + (ok ? "1" : "0")
                + (ok ? "&id=" + Uri.EscapeDataString(id) : string.Empty);
            redirectJs =
                "try{location.replace("
                + System.Text.Json.JsonSerializer.Serialize(dest)
                + ");return;}catch(e){}";
        }

        var html =
            "<!DOCTYPE html>"
            + "<html lang=\"en\"><head><meta charset=\"utf-8\"/>"
            + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>"
            + "<title>Payment processed</title>"
            + "<style>body{font-family:system-ui,sans-serif;display:grid;place-items:center;"
            + "min-height:100vh;margin:0;background:#f7f7f8;color:#1a1a1a}"
            + "main{text-align:center;padding:24px}h1{font-size:1.25rem;margin-bottom:8px}"
            + "p{color:#555}</style>"
            + "<script>(function(){"
            + "var q=new URLSearchParams(location.search);"
            + "var id=(q.get('id')||q.get('payment_id')||'').trim();"
            + "var ok=!!id;"
            + "var msg={type:'qalam-payment-return',provider:"
            + System.Text.Json.JsonSerializer.Serialize(safeProvider)
            + ",id:id,ok:ok};"
            + "try{if(window.opener&&!window.opener.closed){"
            + "window.opener.postMessage(msg,'*');"
            + "try{window.opener.focus();}catch(e){}"
            + "setTimeout(function(){try{window.close();}catch(e){}},250);"
            + "return;}}catch(e){}"
            + (redirectJs ?? string.Empty)
            + "})();</script>"
            + "</head><body><main>"
            + "<h1>Payment processed</h1>"
            + "<p>You can return to the Qalam app. (" + encoded + ")</p>"
            + "<p style=\"font-size:0.9rem;margin-top:16px\">If this tab did not close, switch back to Qalam.</p>"
            + "</main></body></html>";

        return Content(html, "text/html", Encoding.UTF8);
    }
}
