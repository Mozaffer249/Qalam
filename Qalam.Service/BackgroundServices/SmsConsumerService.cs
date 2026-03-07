using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using System.Text;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Qalam.Service.BackgroundServices
{
    public class SmsConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SmsConsumerService> _logger;
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly SmsSettings _smsSettings;
        private IConnection? _connection;
        private IChannel? _channel;

        public SmsConsumerService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SmsConsumerService> logger,
            IOptions<RabbitMQSettings> rabbitMQSettings,
            IOptions<SmsSettings> smsSettings)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _rabbitMQSettings = rabbitMQSettings.Value;
            _smsSettings = smsSettings.Value;

            if (!string.IsNullOrEmpty(_smsSettings.AccountSid) && !string.IsNullOrEmpty(_smsSettings.AuthToken))
            {
                TwilioClient.Init(_smsSettings.AccountSid, _smsSettings.AuthToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SMS Consumer Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = _rabbitMQSettings.HostName,
                        Port = _rabbitMQSettings.Port,
                        UserName = _rabbitMQSettings.UserName,
                        Password = _rabbitMQSettings.Password,
                        VirtualHost = _rabbitMQSettings.VirtualHost
                    };

                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}. Retrying in 30 seconds...",
                        _rabbitMQSettings.HostName, _rabbitMQSettings.Port);
                    try { await Task.Delay(30000, stoppingToken); }
                    catch (TaskCanceledException) { return; }
                }
            }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await _channel!.QueueDeclareAsync(
                    queue: _rabbitMQSettings.SmsQueueName, durable: true, exclusive: false,
                    autoDelete: false, arguments: null, cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);

                    try
                    {
                        var smsMessage = JsonSerializer.Deserialize<SmsMessage>(messageJson);
                        if (smsMessage != null)
                        {
                            _logger.LogInformation("Processing queued SMS to: {PhoneNumber}", smsMessage.PhoneNumber);
                            await SendSmsDirectlyAsync(smsMessage);
                            _logger.LogInformation("Successfully processed queued SMS to: {PhoneNumber}", smsMessage.PhoneNumber);
                        }
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process queued SMS. Message: {Message}", messageJson);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: _rabbitMQSettings.SmsQueueName, autoAck: false,
                    consumer: consumer, cancellationToken: stoppingToken);

                _logger.LogInformation("SMS Consumer Service started. Listening on queue: {Queue}", _rabbitMQSettings.SmsQueueName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("SMS Consumer Service is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS Consumer Service encountered an error");
                throw;
            }
        }

        private async Task SendSmsDirectlyAsync(SmsMessage smsMessage)
        {
            var message = await MessageResource.CreateAsync(
                body: smsMessage.Content,
                from: new PhoneNumber(_smsSettings.FromNumber),
                to: new PhoneNumber(smsMessage.PhoneNumber));

            _logger.LogInformation("SMS sent successfully. SID: {Sid}", message.Sid);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SMS Consumer Service stopping...");
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
