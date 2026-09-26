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
/// Consumes <see cref="OpenSessionRequestAttachmentUploadMessage"/> messages from RabbitMQ and uploads
/// the file to OSS at the pre-computed <c>StorageKey</c>. The API handler already saved
/// the StorageKey + PublicUrl on the attachment row before queueing, so no cross-DB write
/// is needed here.
/// </summary>
public class OpenSessionRequestAttachmentConsumer : RabbitMqConsumerBase
{
    public const string LivenessName = "OpenSessionRequestAttachment";

    private readonly ILogger<OpenSessionRequestAttachmentConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OpenSessionRequestAttachmentConsumer(
        ILogger<OpenSessionRequestAttachmentConsumer> logger,
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
            queue: Settings.OpenSessionRequestAttachmentUploadQueueName,
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
                var message = JsonSerializer.Deserialize<OpenSessionRequestAttachmentUploadMessage>(body);

                if (message != null && !string.IsNullOrEmpty(message.StorageKey))
                {
                    _logger.LogInformation("========== OPEN SESSION REQUEST ATTACHMENT ==========");
                    _logger.LogInformation(
                        "Received: RequestId={RequestId}, AttachmentId={AttachmentId}, File={FileName}, Key={Key}, Size={Size}bytes",
                        message.OpenSessionRequestId, message.AttachmentId,
                        message.FileName, message.StorageKey, message.FileData.Length);

                    using var scope = _scopeFactory.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

                    var fileBytes = Convert.FromBase64String(message.FileData);
                    using var stream = new MemoryStream(fileBytes);
                    _logger.LogInformation("Decoded base64 → {ByteCount} bytes", fileBytes.Length);

                    var fileUrl = await storageService.UploadFileAsync(
                        message.StorageKey, stream, message.ContentType, OssBucketKeys.Learning);

                    _logger.LogInformation("OSS upload SUCCESS: {Url}", fileUrl);
                    _logger.LogInformation("========== OPEN SESSION REQUEST ATTACHMENT COMPLETE ==========");
                }
                else
                {
                    _logger.LogWarning("Open-session-request attachment message had empty StorageKey — skipping");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process open session request attachment upload");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.OpenSessionRequestAttachmentUploadQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "OpenSessionRequestAttachmentConsumer listening on queue: {Queue}",
            Settings.OpenSessionRequestAttachmentUploadQueueName);
    }
}
