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

public class ProfilePicUploadConsumer : RabbitMqConsumerBase
{
    public const string LivenessName = "ProfilePicUpload";

    private readonly ILogger<ProfilePicUploadConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProfilePicUploadConsumer(
        ILogger<ProfilePicUploadConsumer> logger,
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
            queue: Settings.ProfilePicUploadQueueName,
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
                var message = JsonSerializer.Deserialize<ProfilePicUploadMessage>(body);

                if (message != null)
                {
                    _logger.LogInformation("========== PROFILE PIC UPLOAD ==========");
                    _logger.LogInformation("Received: UserId={UserId}, File={FileName}, Size={Size}bytes",
                        message.UserId, message.FileName, message.FileData.Length);

                    using var scope = _scopeFactory.CreateScope();
                    var storageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

                    var fileBytes = Convert.FromBase64String(message.FileData);
                    using var stream = new MemoryStream(fileBytes);
                    _logger.LogInformation("Decoded base64 → {ByteCount} bytes", fileBytes.Length);

                    var extension = Path.GetExtension(message.FileName);
                    var key = $"profiles/{message.UserId}/{Guid.NewGuid()}{extension}";
                    _logger.LogInformation("Uploading to OSS key: {Key}", key);

                    var fileUrl = await storageService.UploadFileAsync(key, stream, message.ContentType);
                    _logger.LogInformation("OSS upload SUCCESS: {Url}", fileUrl);

                    if (message.UserId > 0)
                    {
                        await dbContext.Database.ExecuteSqlRawAsync(
                            "UPDATE AspNetUsers SET ProfilePictureUrl = {0} WHERE Id = {1}",
                            fileUrl, message.UserId);
                        _logger.LogInformation("DB updated: AspNetUsers.Id={UserId} → ProfilePictureUrl={Url}",
                            message.UserId, fileUrl);
                    }
                    else
                    {
                        _logger.LogInformation("UserId=0 (test mode) — skipped DB update");
                    }

                    var previous = message.PreviousFileUrl?.Trim();
                    if (!string.IsNullOrEmpty(previous)
                        && !string.Equals(previous, fileUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            await storageService.DeleteFileAsync(previous);
                            _logger.LogInformation(
                                "Previous profile pic deleted from OSS: {Url}",
                                previous);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(
                                deleteEx,
                                "Failed to delete previous profile pic (upload succeeded): {Url}",
                                previous);
                        }
                    }

                    _logger.LogInformation("========== PROFILE PIC COMPLETE ==========");
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process profile pic upload");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.ProfilePicUploadQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("ProfilePicUploadConsumer listening on queue: {Queue}", Settings.ProfilePicUploadQueueName);
    }
}
