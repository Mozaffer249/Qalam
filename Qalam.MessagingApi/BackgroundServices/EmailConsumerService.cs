using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.BackgroundServices;

public class EmailConsumerService : BackgroundService
{
    private readonly ILogger<EmailConsumerService> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly EmailSettings _emailSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public EmailConsumerService(
        ILogger<EmailConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<EmailSettings> emailSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _emailSettings = emailSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailConsumerService starting...");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitSettings.HostName,
                Port = _rabbitSettings.Port,
                UserName = _rabbitSettings.UserName,
                Password = _rabbitSettings.Password,
                VirtualHost = _rabbitSettings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: _rabbitSettings.EmailQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var messageId = Guid.NewGuid().ToString();
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var emailMessage = JsonSerializer.Deserialize<EmailMessage>(body);

                    if (emailMessage != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();

                        await trackingService.LogMessageAsync(messageId, MessageType.Email,
                            emailMessage.To, emailMessage.Subject, emailMessage.Body, MessageStatus.Processing);

                        await SendEmailDirectAsync(emailMessage);

                        await trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);
                        _logger.LogInformation("Email consumed and sent to: {To}", emailMessage.To);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process email message");

                    using var scope = _scopeFactory.CreateScope();
                    var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
                    await trackingService.UpdateStatusAsync(messageId, MessageStatus.Failed, ex.Message);

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.EmailQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("EmailConsumerService listening on queue: {Queue}", _rabbitSettings.EmailQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("EmailConsumerService stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailConsumerService encountered an error");
        }
    }

    private async Task SendEmailDirectAsync(EmailMessage emailMessage)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(emailMessage.To));
        mimeMessage.Subject = emailMessage.Subject;

        var builder = new BodyBuilder();
        if (emailMessage.IsHtml)
            builder.HtmlBody = emailMessage.Body;
        else
            builder.TextBody = emailMessage.Body;
        mimeMessage.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        var secureOption = _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, secureOption);

        if (!string.IsNullOrEmpty(_emailSettings.UserName))
            await smtp.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);

        await smtp.SendAsync(mimeMessage);
        await smtp.DisconnectAsync(true);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
