using System.Net;
using System.Text;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Contact.Commands.SubmitContactMessage;

public class SubmitContactMessageCommandHandler : ResponseHandler,
    IRequestHandler<SubmitContactMessageCommand, Response<string>>
{
    private const string DefaultInbox = "info@qalam.net.sa";

    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public SubmitContactMessageCommandHandler(
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
    }

    public async Task<Response<string>> Handle(
        SubmitContactMessageCommand request,
        CancellationToken cancellationToken)
    {
        var inbox = string.IsNullOrWhiteSpace(_emailSettings.FromEmail)
            ? DefaultInbox
            : _emailSettings.FromEmail.Trim();

        var subject = $"[Qalam Contact] {request.Name.Trim()}";
        var body = BuildHtmlBody(request);

        try
        {
            await _emailService.SendEmailAsync(inbox, subject, body, SendingStrategy.Queued);
            return Success<string>("Your message was sent successfully.");
        }
        catch (Exception)
        {
            return BadRequest<string>("Unable to send your message. Please try again later.");
        }
    }

    private static string BuildHtmlBody(SubmitContactMessageCommand request)
    {
        var sb = new StringBuilder();
        sb.Append("<h2>New contact form message</h2>");
        sb.Append("<table style=\"border-collapse:collapse;font-family:sans-serif;font-size:14px;\">");
        AppendRow(sb, "Name", request.Name);
        AppendRow(sb, "Phone", request.Phone);
        AppendRow(sb, "Email", string.IsNullOrWhiteSpace(request.Email) ? "—" : request.Email!);
        sb.Append("</table>");
        sb.Append("<h3>Message</h3>");
        sb.Append("<p style=\"white-space:pre-wrap;font-family:sans-serif;font-size:14px;\">");
        sb.Append(WebUtility.HtmlEncode(request.Message.Trim()));
        sb.Append("</p>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<tr>");
        sb.Append("<td style=\"padding:4px 12px 4px 0;font-weight:bold;vertical-align:top;\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</td>");
        sb.Append("<td style=\"padding:4px 0;\">");
        sb.Append(WebUtility.HtmlEncode(value.Trim()));
        sb.Append("</td>");
        sb.Append("</tr>");
    }
}
