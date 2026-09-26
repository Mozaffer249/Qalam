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

public class SmsConsumerService : RabbitMqConsumerBase
{
    public const string LivenessName = "Sms";

    private readonly ILogger<SmsConsumerService> _logger;
    private readonly SmsSettings _smsSettings;
    private readonly IServiceScopeFactory _scopeFactory;

    public SmsConsumerService(
        ILogger<SmsConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<SmsSettings> smsSettings,
        IServiceScopeFactory scopeFactory,
        IConsumerLivenessTracker liveness)
        : base(rabbitSettings, liveness, logger)
    {
        _logger = logger;
        _smsSettings = smsSettings.Value;
        _scopeFactory = scopeFactory;

        if (!string.IsNullOrEmpty(_smsSettings.AccountSid) && !string.IsNullOrEmpty(_smsSettings.AuthToken))
            TwilioClient.Init(_smsSettings.AccountSid, _smsSettings.AuthToken);
    }

    protected override string ConsumerName => LivenessName;

    protected override async Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken)
    {
        await channel.QueueDeclareAsync(
            queue: Settings.SmsQueueName,
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

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process SMS message");

                using var scope = _scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<IMessageTrackingService>();
                await trackingService.UpdateStatusAsync(messageId, MessageStatus.Failed, ex.Message);

                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.SmsQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("SmsConsumerService listening on queue: {Queue}", Settings.SmsQueueName);
    }
}
