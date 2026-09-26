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
/// Consumes course cover image uploads and stores them on the learning OSS bucket
/// at the pre-computed <c>StorageKey</c>. The API already returned the public URL to the client.
/// </summary>
public class CourseImageUploadConsumer : RabbitMqConsumerBase
{
    public const string LivenessName = "CourseImageUpload";

    private readonly ILogger<CourseImageUploadConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CourseImageUploadConsumer(
        ILogger<CourseImageUploadConsumer> logger,
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
            queue: Settings.CourseImageUploadQueueName,
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
                var message = JsonSerializer.Deserialize<CourseImageUploadMessage>(body);

                if (message != null && !string.IsNullOrEmpty(message.StorageKey))
                {
                    _logger.LogInformation("========== COURSE IMAGE UPLOAD ==========");
                    _logger.LogInformation(
                        "Received: TeacherId={TeacherId}, File={FileName}, Key={Key}, Size={Size}bytes",
                        message.TeacherId, message.FileName, message.StorageKey, message.FileData.Length);

                    using var scope = _scopeFactory.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

                    var fileBytes = Convert.FromBase64String(message.FileData);
                    using var stream = new MemoryStream(fileBytes);
                    _logger.LogInformation("Decoded base64 → {ByteCount} bytes", fileBytes.Length);

                    var fileUrl = await storageService.UploadFileAsync(
                        message.StorageKey, stream, message.ContentType, OssBucketKeys.Learning);

                    _logger.LogInformation("OSS upload SUCCESS: {Url}", fileUrl);
                    _logger.LogInformation("========== COURSE IMAGE UPLOAD COMPLETE ==========");
                }
                else
                {
                    _logger.LogWarning("Course image message had empty StorageKey — skipping");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process course image upload");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.CourseImageUploadQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "CourseImageUploadConsumer listening on queue: {Queue}",
            Settings.CourseImageUploadQueueName);
    }
}
