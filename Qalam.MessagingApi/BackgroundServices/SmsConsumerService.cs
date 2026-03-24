using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;
using System.Text;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Qalam.MessagingApi.BackgroundServices;

public class SmsConsumerService : BackgroundService
{
    private readonly ILogger<SmsConsumerService> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly SmsSettings _smsSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public SmsConsumerService(
        ILogger<SmsConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<SmsSettings> smsSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _smsSettings = smsSettings.Value;
        _scopeFactory = scopeFactory;

        if (!string.IsNullOrEmpty(_smsSettings.AccountSid) && !string.IsNullOrEmpty(_smsSettings.AuthToken))
            TwilioClient.Init(_smsSettings.AccountSid, _smsSettings.AuthToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SmsConsumerService starting...");

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
                queue: _rabbitSettings.SmsQueueName,
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
                    var smsMessage = JsonSerializer.Deserialize<SmsMessage>(body);

                    if (smsMessage != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();

                        await trackingService.LogMessageAsync(messageId, MessageType.SMS,
                            smsMessage.PhoneNumber, "SMS", smsMessage.Content, MessageStatus.Processing);

                        var message = await MessageResource.CreateAsync(
                            body: smsMessage.Content,
                            from: new PhoneNumber(_smsSettings.FromNumber),
                            to: new PhoneNumber(smsMessage.PhoneNumber));

                        await trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);
                        _logger.LogInformation("SMS consumed and sent to: {PhoneNumber}, SID: {Sid}",
                            smsMessage.PhoneNumber, message.Sid);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process SMS message");

                    using var scope = _scopeFactory.CreateScope();
                    var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
                    await trackingService.UpdateStatusAsync(messageId, MessageStatus.Failed, ex.Message);

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.SmsQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("SmsConsumerService listening on queue: {Queue}", _rabbitSettings.SmsQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("SmsConsumerService stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmsConsumerService encountered an error");
        }
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
