using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.BackgroundServices;

public class TeacherDocUploadConsumer : RabbitMqConsumerBase
{
    public const string LivenessName = "TeacherDocUpload";

    private readonly ILogger<TeacherDocUploadConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TeacherDocUploadConsumer(
        ILogger<TeacherDocUploadConsumer> logger,
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
            queue: Settings.TeacherDocUploadQueueName,
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
                var message = JsonSerializer.Deserialize<TeacherDocUploadMessage>(body);

                if (message != null)
                {
                    _logger.LogInformation("========== TEACHER DOC UPLOAD ==========");
                    _logger.LogInformation("Received: Teacher={TeacherId}, DocId={DocumentId}, File={FileName}, Size={Size}bytes",
                        message.TeacherId, message.DocumentId, message.FileName, message.FileData.Length);

                    using var scope = _scopeFactory.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

                    var fileBytes = Convert.FromBase64String(message.FileData);
                    using var stream = new MemoryStream(fileBytes);
                    _logger.LogInformation("Decoded base64 → {ByteCount} bytes", fileBytes.Length);

                    var extension = Path.GetExtension(message.FileName);
                    var key = $"teachers/{message.TeacherId}/{message.DocumentType}/{Guid.NewGuid()}{extension}";
                    _logger.LogInformation("Uploading to OSS key: {Key}", key);

                    var fileUrl = await storageService.UploadFileAsync(key, stream, message.ContentType);
                    _logger.LogInformation("OSS upload SUCCESS: {Url}", fileUrl);

                    if (message.DocumentId > 0)
                    {
                        await dbContext.Database.ExecuteSqlRawAsync(
                            "UPDATE dbo.TeacherDocuments SET FilePath = {0} WHERE Id = {1}",
                            fileUrl, message.DocumentId);
                        _logger.LogInformation("DB updated: TeacherDocuments.Id={DocumentId} → FilePath={Url}",
                            message.DocumentId, fileUrl);
                    }
                    else
                    {
                        _logger.LogInformation("DocumentId=0 (test mode) — skipped DB update");
                    }

                    _logger.LogInformation("========== TEACHER DOC COMPLETE ==========");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                if (ea.Redelivered)
                {
                    _logger.LogError(ex,
                        "Failed to process teacher doc upload (redelivered — dropping to avoid loop). " +
                        "Investigate manually. DeliveryTag={Tag}", ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
                    _logger.LogError(ex, "Failed to process teacher doc upload (first attempt — requeuing once)");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.TeacherDocUploadQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("TeacherDocUploadConsumer listening on queue: {Queue}", Settings.TeacherDocUploadQueueName);
    }
}
