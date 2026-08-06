using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Qalam.MessagingApi.Configuration;
using static Qalam.MessagingApi.Configuration.SmtpSecureSocketOptions;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IEmailSuppressionService _suppressionService;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IMessageQueueService messageQueueService,
        IEmailSuppressionService suppressionService)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _messageQueueService = messageQueueService;
        _suppressionService = suppressionService;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        await SendEmailAsync(email, subject, message, _emailSettings.DefaultStrategy);
    }

    public async Task SendEmailAsync(string email, string subject, string message, SendingStrategy strategy)
    {
        EnsureValidEmailAddress(email);

        if (await _suppressionService.IsSuppressedAsync(email))
        {
            _logger.LogWarning("Skipping email to suppressed address: {Email}", email);
            return;
        }

        switch (strategy)
        {
            case SendingStrategy.Direct:
                await SendDirectAsync(email, subject, message);
                break;
            case SendingStrategy.Queued:
                await QueueEmailAsync(email, subject, message);
                break;
            case SendingStrategy.Fallback:
                await SendWithFallbackAsync(email, subject, message);
                break;
            default:
                throw new ArgumentException($"Unknown email sending strategy: {strategy}");
        }
    }

    private async Task SendDirectAsync(string email, string subject, string body)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(email));
            mimeMessage.Subject = subject;
            SystemEmailMime.ApplySystemMailHeaders(mimeMessage, _emailSettings);

            var builder = new BodyBuilder
            {
                HtmlBody = SystemEmailMime.EnsureDoNotReplyHtmlFooter(body)
            };
            mimeMessage.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, FromEmailSettings(_emailSettings));

            if (!string.IsNullOrEmpty(_emailSettings.UserName))
                await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);

            await smtp.SendAsync(mimeMessage);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully (Direct) to: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {Email}", email);
            throw;
        }
    }

    private async Task QueueEmailAsync(string email, string subject, string body)
    {
        await _messageQueueService.QueueEmailAsync(new EmailMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            To = email,
            Subject = subject,
            Body = body,
            IsHtml = true
        });
        _logger.LogInformation("Email queued for delivery to: {Email}", email);
    }

    private async Task SendWithFallbackAsync(string email, string subject, string body)
    {
        try
        {
            await SendDirectAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct email failed to: {Email}, falling back to queue", email);
            try
            {
                await QueueEmailAsync(email, subject, body);
            }
            catch (Exception queueEx)
            {
                _logger.LogError(queueEx, "Failed to queue email to: {Email}. Email will be lost.", email);
                throw;
            }
        }
    }

    private static void EnsureValidEmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailboxAddress.TryParse(email, out _))
            throw new ArgumentException($"Invalid email address: '{email}'", nameof(email));
    }
}
