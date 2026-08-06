using MimeKit;

namespace Qalam.MessagingApi.Configuration;

/// <summary>
/// Applies no-reply Reply-To, auto-submitted headers, and a do-not-reply body footer
/// for all system-generated outbound mail.
/// </summary>
public static class SystemEmailMime
{
    public const string DoNotReplyMarker = "data-qalam-noreply";

    private const string DoNotReplyHtmlFooter =
        """<p style="margin-top:24px;font-size:12px;color:#666;" data-qalam-noreply="1">This is an automated message; please do not reply.</p>""";

    private const string DoNotReplyTextFooter =
        "\n\n---\nThis is an automated message; please do not reply.\n";

    public static void ApplySystemMailHeaders(MimeMessage message, EmailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(settings);

        var replyTo = ResolveReplyTo(settings.ReplyToEmail, settings.FromEmail);
        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            message.ReplyTo.Clear();
            message.ReplyTo.Add(new MailboxAddress(string.Empty, replyTo));
        }

        // Replace if already present so Direct + Consumer stay idempotent.
        message.Headers.RemoveAll("Auto-Submitted");
        message.Headers.Add("Auto-Submitted", "auto-generated");
        message.Headers.RemoveAll("X-Auto-Response-Suppress");
        message.Headers.Add("X-Auto-Response-Suppress", "All");
    }

    public static string EnsureDoNotReplyHtmlFooter(string? htmlBody)
    {
        var body = htmlBody ?? string.Empty;
        if (body.Contains(DoNotReplyMarker, StringComparison.OrdinalIgnoreCase))
            return body;
        return body + DoNotReplyHtmlFooter;
    }

    public static string EnsureDoNotReplyTextFooter(string? textBody)
    {
        var body = textBody ?? string.Empty;
        if (body.Contains("please do not reply", StringComparison.OrdinalIgnoreCase))
            return body;
        return body + DoNotReplyTextFooter;
    }

    /// <summary>
    /// Explicit ReplyToEmail wins; otherwise noreply@&lt;FromEmail domain&gt;.
    /// Skips Reply-To when From is already a noreply address and ReplyTo is empty.
    /// </summary>
    public static string? ResolveReplyTo(string? replyToEmail, string? fromEmail)
    {
        if (!string.IsNullOrWhiteSpace(replyToEmail))
            return replyToEmail.Trim();

        if (string.IsNullOrWhiteSpace(fromEmail))
            return null;

        var from = fromEmail.Trim();
        var at = from.IndexOf('@');
        if (at <= 0 || at >= from.Length - 1)
            return null;

        var local = from[..at];
        if (local.Equals("noreply", StringComparison.OrdinalIgnoreCase)
            || local.Equals("no-reply", StringComparison.OrdinalIgnoreCase))
            return null;

        return "noreply@" + from[(at + 1)..];
    }
}
