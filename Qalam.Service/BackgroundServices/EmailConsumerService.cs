using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using System.Text;
using System.Text.Json;

namespace Qalam.Service.BackgroundServices
{
    public class EmailConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EmailConsumerService> _logger;
        private readonly RabbitMQSettings _settings;
        private IConnection? _connection;
        private IChannel? _channel;

        public EmailConsumerService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EmailConsumerService> logger,
            IOptions<RabbitMQSettings> settings)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Consumer Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = _settings.HostName,
                        Port = _settings.Port,
                        UserName = _settings.UserName,
                        Password = _settings.Password,
                        VirtualHost = _settings.VirtualHost
                    };

                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}. Retrying in 30 seconds...",
                        _settings.HostName, _settings.Port);
                    try { await Task.Delay(30000, stoppingToken); }
                    catch (TaskCanceledException) { return; }
                }
            }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await _channel!.QueueDeclareAsync(
                    queue: _settings.EmailQueueName, durable: true, exclusive: false,
                    autoDelete: false, arguments: null, cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);

                    try
                    {
                        var emailMessage = JsonSerializer.Deserialize<EmailMessage>(messageJson);
                        if (emailMessage != null)
                        {
                            _logger.LogInformation("Processing queued email to: {To}", emailMessage.To);
                            using var scope = _serviceScopeFactory.CreateScope();
                            await SendEmailDirectlyAsync(emailMessage, scope);
                            _logger.LogInformation("Successfully processed queued email to: {To}", emailMessage.To);
                        }
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process queued email. Message: {Message}", messageJson);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: _settings.EmailQueueName, autoAck: false,
                    consumer: consumer, cancellationToken: stoppingToken);

                _logger.LogInformation("Email Consumer Service started. Listening on queue: {Queue}", _settings.EmailQueueName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Email Consumer Service is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email Consumer Service encountered an error");
                throw;
            }
        }

        private async Task SendEmailDirectlyAsync(EmailMessage emailMessage, IServiceScope scope)
        {
            var emailSettings = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(emailSettings.Host, emailSettings.Port,
                emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await smtpClient.AuthenticateAsync(emailSettings.UserName, emailSettings.Password);

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(emailMessage.To));
            mimeMessage.Subject = emailMessage.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = emailMessage.Body,
                TextBody = emailMessage.Body
            };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            await smtpClient.SendAsync(mimeMessage);
            await smtpClient.DisconnectAsync(true);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email Consumer Service stopping...");
            if (_channel != null) { await _channel.CloseAsync(cancellationToken); await _channel.DisposeAsync(); }
            if (_connection != null) { await _connection.CloseAsync(cancellationToken); await _connection.DisposeAsync(); }
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
