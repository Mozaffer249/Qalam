using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Payments;

/// <summary>
/// Browser return landing page for hosted payment gateways.
/// Flutter WebViews intercept this URL prefix and call Confirm.
/// On web (new tab), posts a message to the opener and tries to close itself.
/// </summary>
[ApiController]
[AllowAnonymous]
public class PaymentReturnController : ControllerBase
{
    [HttpGet(Router.PaymentReturn)]
    [Produces("text/html")]
    public ContentResult Return(string provider)
    {
        var safeProvider = string.IsNullOrWhiteSpace(provider) ? "payment" : provider.Trim();
        var encoded = WebUtility.HtmlEncode(safeProvider);
        // Moyasar appends ?id=<payment_id> on success_url.
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
            + "var msg={type:'qalam-payment-return',provider:"
            + System.Text.Json.JsonSerializer.Serialize(safeProvider)
            + ",id:id};"
            + "try{if(window.opener&&!window.opener.closed){"
            + "window.opener.postMessage(msg,'*');"
            + "try{window.opener.focus();}catch(e){}"
            + "setTimeout(function(){try{window.close();}catch(e){}},250);"
            + "return;}}catch(e){}"
            + "})();</script>"
            + "</head><body><main>"
            + "<h1>Payment processed</h1>"
            + "<p>You can return to the Qalam app. (" + encoded + ")</p>"
            + "<p style=\"font-size:0.9rem;margin-top:16px\">If this tab did not close, switch back to Qalam.</p>"
            + "</main></body></html>";

        return Content(html, "text/html", Encoding.UTF8);
    }
}
