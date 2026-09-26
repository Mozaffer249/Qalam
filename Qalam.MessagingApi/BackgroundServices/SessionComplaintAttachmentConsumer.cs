using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.BackgroundServices;

/// <summary>
/// Consumes <see cref="SessionComplaintAttachmentUploadMessage"/> and uploads files to OSS
/// at the pre-computed storage key. The API stores the public URL before queueing.
/// </summary>
public class SessionComplaintAttachmentConsumer : RabbitMqConsumerBase
{
    public const string LivenessName = "SessionComplaintAttachment";

    private readonly ILogger<SessionComplaintAttachmentConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SessionComplaintAttachmentConsumer(
        ILogger<SessionComplaintAttachmentConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IServiceScopeFactory scopeFactory,
        IConsumerLivenessTracker liveness)
        : base(rabbitSettings, liveness, logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override string ConsumerName => LivenessName;

    protected override async Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken)
    {
        await channel.QueueDeclareAsync(
            queue: Settings.SessionComplaintAttachmentUploadQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<SessionComplaintAttachmentUploadMessage>(body);

                if (message != null && !string.IsNullOrEmpty(message.StorageKey))
                {
                    _logger.LogInformation(
                        "Session complaint attachment: ComplaintId={ComplaintId}, AttachmentId={AttachmentId}, Key={Key}",
                        message.ComplaintId, message.AttachmentId, message.StorageKey);

                    using var scope = _scopeFactory.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

                    var fileBytes = Convert.FromBase64String(message.FileData);
                    using var stream = new MemoryStream(fileBytes);
                    var fileUrl = await storageService.UploadFileAsync(
                        message.StorageKey, stream, message.ContentType, OssBucketKeys.Learning);

                    _logger.LogInformation("Session complaint attachment OSS upload SUCCESS: {Url}", fileUrl);
                }
                else
                {
                    _logger.LogWarning("Session complaint attachment message had empty StorageKey — skipping");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process session complaint attachment upload");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.SessionComplaintAttachmentUploadQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "SessionComplaintAttachmentConsumer listening on queue: {Queue}",
            Settings.SessionComplaintAttachmentUploadQueueName);
    }
}
