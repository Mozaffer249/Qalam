using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.BackgroundServices;

/// <summary>
/// Polls the NDR/bounce mailbox via IMAP and suppresses permanently bounced addresses.
/// Disabled unless BounceIngestSettings.Enabled is true and IMAP host is configured.
/// </summary>
public class BounceIngestionService : BackgroundService
{
    private static readonly Regex FinalRecipientRegex = new(
        @"Final-Recipient:\s*(?:rfc822;)?\s*(?<email>[^\s;]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StatusRegex = new(
        @"Status:\s*(?<code>\d\.\d\.\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DiagnosticRegex = new(
        @"(?:Diagnostic-Code|SMTP error)[^\n]*?(?<code>5\d\d|4\d\d)[^\n]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AngleEmailRegex = new(
        @"<?(?<email>[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,})>?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<BounceIngestionService> _logger;
    private readonly BounceIngestSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;

    public BounceIngestionService(
        ILogger<BounceIngestionService> logger,
        IOptions<BounceIngestSettings> settings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogInformation(
                "BounceIngestionService disabled (Enabled={Enabled}, Host configured={HasHost})",
                _settings.Enabled, !string.IsNullOrWhiteSpace(_settings.Host));
            return;
        }

        _logger.LogInformation(
            "BounceIngestionService starting (host={Host}, interval={Minutes}m)",
            _settings.Host, _settings.PollIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BounceIngestionService poll failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _settings.PollIntervalMinutes)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        using var client = new ImapClient();
        var secure = _settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_settings.Host, _settings.Port, secure, cancellationToken);
        await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        IMailFolder? processedFolder = null;
        if (!string.IsNullOrWhiteSpace(_settings.ProcessedFolder))
        {
            try
            {
                processedFolder = await client.GetFolderAsync(_settings.ProcessedFolder, cancellationToken);
            }
            catch
            {
                // Folder may not exist — fall back to \Seen only.
                _logger.LogWarning("Processed folder '{Folder}' not found; marking Seen only",
                    _settings.ProcessedFolder);
            }
        }

        var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
        _logger.LogInformation("BounceIngestionService found {Count} unseen messages", uids.Count);

        using var scope = _scopeFactory.CreateScope();
        var suppression = scope.ServiceProvider.GetRequiredService<IEmailSuppressionService>();

        foreach (var uid in uids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                var results = ParseBounce(message);

                foreach (var result in results.Where(r => r.IsPermanent && !string.IsNullOrWhiteSpace(r.Email)))
                {
                    await suppression.SuppressAsync(
                        result.Email!,
                        ClassifyReason(result.StatusCode, result.Diagnostic),
                        EmailSuppressionSource.BounceIngest,
                        result.Diagnostic,
                        cancellationToken);
                }

                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                if (processedFolder != null)
                {
                    await inbox.MoveToAsync(uid, processedFolder, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process bounce message uid={Uid}", uid);
            }
        }

        await client.DisconnectAsync(true, cancellationToken);
    }

    internal static List<BounceParseResult> ParseBounce(MimeMessage message)
    {
        var results = new List<BounceParseResult>();
        var textParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(message.TextBody))
            textParts.Add(message.TextBody);

        CollectDeliveryStatus(message.Body, textParts);

        var blob = string.Join("\n", textParts);
        if (string.IsNullOrWhiteSpace(blob))
            blob = message.Subject ?? string.Empty;

        var emails = FinalRecipientRegex.Matches(blob)
            .Select(m => m.Groups["email"].Value.Trim().Trim('<', '>'))
            .Where(e => e.Contains('@'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
        {
            // Fallback: first email-looking token after "failed" / RCPT TO
            var rcpt = Regex.Match(blob, @"RCPT TO:<(?<email>[^>]+)>", RegexOptions.IgnoreCase);
            if (rcpt.Success)
                emails.Add(rcpt.Groups["email"].Value.Trim());
            else
            {
                var any = AngleEmailRegex.Match(blob);
                if (any.Success)
                    emails.Add(any.Groups["email"].Value.Trim());
            }
        }

        var statusMatch = StatusRegex.Match(blob);
        var statusCode = statusMatch.Success ? statusMatch.Groups["code"].Value : null;
        var diagMatch = DiagnosticRegex.Match(blob);
        var diagnostic = diagMatch.Success ? diagMatch.Value.Trim() : Truncate(blob, 500);

        var isPermanent = IsPermanentStatus(statusCode, blob);

        foreach (var email in emails)
        {
            results.Add(new BounceParseResult
            {
                Email = email,
                StatusCode = statusCode,
                Diagnostic = diagnostic,
                IsPermanent = isPermanent
            });
        }

        return results;
    }

    private static void CollectDeliveryStatus(MimeEntity? entity, List<string> sink)
    {
        if (entity == null)
            return;

        if (entity is MessageDeliveryStatus mds)
        {
            using var stream = new MemoryStream();
            mds.Content.DecodeTo(stream);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            sink.Add(reader.ReadToEnd());
            return;
        }

        if (entity is MessagePart messagePart)
        {
            CollectDeliveryStatus(messagePart.Message.Body, sink);
            return;
        }

        if (entity is Multipart multipart)
        {
            foreach (var part in multipart)
                CollectDeliveryStatus(part, sink);
            return;
        }

        if (entity is TextPart text
            && (text.ContentType.MimeType.Equals("message/delivery-status", StringComparison.OrdinalIgnoreCase)
                || text.ContentType.MimeType.Equals("text/plain", StringComparison.OrdinalIgnoreCase)))
        {
            sink.Add(text.Text);
        }
    }

    private static bool IsPermanentStatus(string? enhancedStatus, string blob)
    {
        if (!string.IsNullOrWhiteSpace(enhancedStatus) && enhancedStatus.StartsWith('5'))
            return true;
        if (!string.IsNullOrWhiteSpace(enhancedStatus) && enhancedStatus.StartsWith('4'))
            return false;

        // Heuristic from SMTP reply codes in body
        if (Regex.IsMatch(blob, @"\b5\d\d[\s\-]", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(blob, @"\b4\d\d[\s\-]", RegexOptions.IgnoreCase))
            return false;

        // Default: treat unknown NDRs as permanent to stop repeated spam
        return blob.Contains("permanent error", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("could not be delivered", StringComparison.OrdinalIgnoreCase);
    }

    private static EmailSuppressionReason ClassifyReason(string? statusCode, string? diagnostic)
    {
        var hay = $"{statusCode} {diagnostic}".ToLowerInvariant();
        if (hay.Contains("5.1.1") || hay.Contains("nosuchuser") || hay.Contains("does not exist"))
            return EmailSuppressionReason.NoSuchUser;
        if (hay.Contains("5.2.2") || hay.Contains("overquota") || hay.Contains("out of storage"))
            return EmailSuppressionReason.OverQuota;
        if (hay.Contains("mailbox unavailable") || hay.Contains("cannot deliver"))
            return EmailSuppressionReason.HardBounce;
        return EmailSuppressionReason.HardBounce;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];

    internal sealed class BounceParseResult
    {
        public string? Email { get; init; }
        public string? StatusCode { get; init; }
        public string? Diagnostic { get; init; }
        public bool IsPermanent { get; init; }
    }
}
