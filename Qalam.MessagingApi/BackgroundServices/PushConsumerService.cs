using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.BackgroundServices;

public class PushConsumerService : BackgroundService
{
    private readonly ILogger<PushConsumerService> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly PushSettings _pushSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public PushConsumerService(
        ILogger<PushConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<PushSettings> pushSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _pushSettings = pushSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PushConsumerService starting...");

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
                queue: _rabbitSettings.PushQueueName,
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
                    var pushMessage = JsonSerializer.Deserialize<PushNotificationMessage>(body);

                    if (pushMessage != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();

                        await trackingService.LogMessageAsync(messageId, MessageType.PushNotification,
                            pushMessage.DeviceToken, pushMessage.Title, pushMessage.Body, MessageStatus.Processing);

                        var firebaseMessage = new Message
                        {
                            Token = pushMessage.DeviceToken,
                            Notification = new Notification
                            {
                                Title = pushMessage.Title,
                                Body = pushMessage.Body
                            },
                            Data = pushMessage.Data?.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value?.ToString() ?? string.Empty)
                        };

                        var response = await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMessage, stoppingToken);

                        await trackingService.UpdateStatusAsync(messageId, MessageStatus.Sent);
                        _logger.LogInformation("Push consumed and sent to: {DeviceToken}, Response: {Response}",
                            pushMessage.DeviceToken, response);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process push notification message");

                    using var scope = _scopeFactory.CreateScope();
                    var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
                    await trackingService.UpdateStatusAsync(messageId, MessageStatus.Failed, ex.Message);

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.PushQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("PushConsumerService listening on queue: {Queue}", _rabbitSettings.PushQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("PushConsumerService stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PushConsumerService encountered an error");
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
