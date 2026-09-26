using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
using static Qalam.MessagingApi.Configuration.SmtpSecureSocketOptions;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.BackgroundServices;

public class EmailConsumerService : RabbitMqConsumerBase
{
    public const string LivenessName = "Email";

    private const string RetryCountHeader = "x-retry-count";
    private const string ErrorHeader = "x-error";
    private const string FailedAtHeader = "x-failed-at";

    private readonly ILogger<EmailConsumerService> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IChannel? _channel;

    public EmailConsumerService(
        ILogger<EmailConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<EmailSettings> emailSettings,
        IServiceScopeFactory scopeFactory,
        IConsumerLivenessTracker liveness)
        : base(rabbitSettings, liveness, logger)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override string ConsumerName => LivenessName;

    protected override async Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken)
    {
        _channel = channel;

        await channel.QueueDeclareAsync(
            queue: Settings.EmailQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: Settings.EmailDeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var retryCount = GetRetryCount(ea.BasicProperties);
            EmailMessage? emailMessage = null;
            string messageId = Guid.NewGuid().ToString();

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                emailMessage = JsonSerializer.Deserialize<EmailMessage>(body);

                if (emailMessage == null)
                {
                    _logger.LogWarning("Discarding null/invalid email payload to DLQ");
                    await PublishToDlqAsync(ea.Body.ToArray(), ea.BasicProperties, "Invalid or null email payload");
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                messageId = string.IsNullOrWhiteSpace(emailMessage.MessageId)
                    ? Guid.NewGuid().ToString()
                    : emailMessage.MessageId;
                emailMessage.MessageId = messageId;

                using var scope = _scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();

                if (retryCount == 0)
                {
                    await trackingService.LogMessageAsync(messageId, MessageType.Email,
                        emailMessage.To, emailMessage.Subject, emailMessage.Body, MessageStatus.Processing);
                }
                else
                {
                    await trackingService.UpdateStatusAsync(messageId, MessageStatus.Processing);
                }

                await SendEmailDirectAsync(emailMessage);
                await trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);
                _logger.LogInformation("Email consumed and sent to: {To}, MessageId: {MessageId}",
                    emailMessage.To, messageId);

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email message (MessageId: {MessageId}, Retry: {Retry})",
                    messageId, retryCount);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
                    await trackingService.UpdateStatusAsync(messageId, MessageStatus.Failed, ex.Message);
                }
                catch (Exception trackEx)
                {
                    _logger.LogWarning(trackEx,
                        "Failed to record email failure (messageId: {MessageId})", messageId);
                }

                var permanent = IsPermanentEmailFailure(ex);
                var maxRetries = Math.Max(0, Settings.EmailMaxRetries);
                var payload = emailMessage != null
                    ? Encoding.UTF8.GetBytes(JsonSerializer.Serialize(emailMessage))
                    : ea.Body.ToArray();

                if (permanent || retryCount >= maxRetries)
                {
                    _logger.LogWarning(
                        "Moving email to DLQ (permanent={Permanent}, retry={Retry}/{Max}, to={To}, messageId={MessageId})",
                        permanent, retryCount, maxRetries, emailMessage?.To, messageId);

                    if (!string.IsNullOrWhiteSpace(emailMessage?.To) && (permanent || retryCount >= maxRetries))
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var suppression = scope.ServiceProvider.GetRequiredService<IEmailSuppressionService>();
                            await suppression.SuppressAsync(
                                emailMessage.To,
                                ClassifyPermanentReason(ex),
                                EmailSuppressionSource.SmtpSend,
                                Truncate(ex.Message, 2000));
                        }
                        catch (Exception suppressEx)
                        {
                            _logger.LogWarning(suppressEx,
                                "Failed to suppress address after permanent failure: {To}", emailMessage?.To);
                        }
                    }

                    await PublishToDlqAsync(payload, ea.BasicProperties, ex.Message);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
                    var nextRetry = retryCount + 1;
                    _logger.LogWarning(
                        "Republishing email for retry {Next}/{Max} (to={To}, messageId={MessageId})",
                        nextRetry, maxRetries, emailMessage?.To, messageId);

                    await RepublishForRetryAsync(payload, nextRetry);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.EmailQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "EmailConsumerService listening on queue: {Queue} (DLQ: {Dlq}, MaxRetries: {MaxRetries})",
            Settings.EmailQueueName,
            Settings.EmailDeadLetterQueueName,
            Settings.EmailMaxRetries);
    }

    private async Task SendEmailDirectAsync(EmailMessage emailMessage)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(emailMessage.To));
        mimeMessage.Subject = emailMessage.Subject;
        SystemEmailMime.ApplySystemMailHeaders(mimeMessage, _emailSettings);

        var builder = new BodyBuilder();
        if (emailMessage.IsHtml)
            builder.HtmlBody = SystemEmailMime.EnsureDoNotReplyHtmlFooter(emailMessage.Body);
        else
            builder.TextBody = SystemEmailMime.EnsureDoNotReplyTextFooter(emailMessage.Body);
        mimeMessage.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, FromEmailSettings(_emailSettings));

        if (!string.IsNullOrEmpty(_emailSettings.UserName))
            await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);

        await smtp.SendAsync(mimeMessage);
        await smtp.DisconnectAsync(true);
    }

    private async Task RepublishForRetryAsync(byte[] body, int nextRetryCount)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                [RetryCountHeader] = nextRetryCount
            }
        };

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: Settings.EmailQueueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private async Task PublishToDlqAsync(byte[] body, IReadOnlyBasicProperties? originalProps, string error)
    {
        var headers = new Dictionary<string, object?>();
        if (originalProps?.Headers != null)
        {
            foreach (var pair in originalProps.Headers)
                headers[pair.Key] = pair.Value;
        }

        headers[RetryCountHeader] = GetRetryCount(originalProps);
        headers[ErrorHeader] = Encoding.UTF8.GetBytes(error);
        headers[FailedAtHeader] = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O"));

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = headers
        };

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: Settings.EmailDeadLetterQueueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers == null ||
            !properties.Headers.TryGetValue(RetryCountHeader, out var raw) ||
            raw is null)
        {
            return 0;
        }

        return raw switch
        {
            int i => i,
            long l => (int)l,
            byte b => b,
            short s => s,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool IsPermanentEmailFailure(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is SmtpCommandException smtpEx)
            {
                var code = (int)smtpEx.StatusCode;
                if (code >= 500 && code < 600)
                    return true;
            }

            if (current is ParseException)
                return true;
        }

        return false;
    }

    private static EmailSuppressionReason ClassifyPermanentReason(Exception ex)
    {
        var message = ex.ToString();
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is SmtpCommandException smtpEx)
                message = $"{(int)smtpEx.StatusCode} {smtpEx.Message} {message}";
        }

        var hay = message.ToLowerInvariant();
        if (hay.Contains("5.1.1") || hay.Contains("does not exist") || hay.Contains("nosuchuser"))
            return EmailSuppressionReason.NoSuchUser;
        if (hay.Contains("5.2.2") || hay.Contains("overquota") || hay.Contains("out of storage"))
            return EmailSuppressionReason.OverQuota;
        if (hay.Contains("parse") || hay.Contains("invalid"))
            return EmailSuppressionReason.InvalidDomain;
        return EmailSuppressionReason.HardBounce;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
