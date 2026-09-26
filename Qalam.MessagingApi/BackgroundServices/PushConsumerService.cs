using FirebaseAdmin.Messaging;
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

public class PushConsumerService : RabbitMqConsumerBase
{
    public const string LivenessName = "Push";

    private readonly ILogger<PushConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PushConsumerService(
        ILogger<PushConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<PushSettings> pushSettings,
        IServiceScopeFactory scopeFactory,
        IConsumerLivenessTracker liveness)
        : base(rabbitSettings, liveness, logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        // pushSettings reserved for Firebase credential bootstrap if DefaultInstance is not yet set
        _ = pushSettings.Value;
    }

    protected override string ConsumerName => LivenessName;

    protected override async Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken)
    {
        await channel.QueueDeclareAsync(
            queue: Settings.PushQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
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

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process push notification message");

                using var scope = _scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
                await trackingService.UpdateStatusAsync(messageId, MessageStatus.Failed, ex.Message);

                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.PushQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("PushConsumerService listening on queue: {Queue}", Settings.PushQueueName);
    }
}
