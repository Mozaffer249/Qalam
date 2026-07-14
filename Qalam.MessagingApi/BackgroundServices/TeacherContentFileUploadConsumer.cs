using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.BackgroundServices;

/// <summary>
/// Consumes teacher content library file upload messages and uploads to OSS,
/// then sets UploadStatus on teacher.TeacherContentItems.
/// </summary>
public class TeacherContentFileUploadConsumer : BackgroundService
{
    private const int UploadStatusReady = 2;
    private const int UploadStatusFailed = 3;

    private readonly ILogger<TeacherContentFileUploadConsumer> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public TeacherContentFileUploadConsumer(
        ILogger<TeacherContentFileUploadConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TeacherContentFileUploadConsumer starting...");

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
                queue: _rabbitSettings.TeacherContentFileUploadQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonSerializer.Deserialize<TeacherContentFileUploadMessage>(body);

                    if (message != null && !string.IsNullOrEmpty(message.StorageKey))
                    {
                        _logger.LogInformation("========== TEACHER CONTENT FILE UPLOAD ==========");
                        _logger.LogInformation(
                            "Received: TeacherId={TeacherId}, ItemId={ItemId}, File={FileName}, Key={Key}",
                            message.TeacherId, message.ContentItemId, message.FileName, message.StorageKey);

                        using var scope = _scopeFactory.CreateScope();
                        var storageService = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

                        var fileBytes = Convert.FromBase64String(message.FileData);
                        using var stream = new MemoryStream(fileBytes);

                        var fileUrl = await storageService.UploadFileAsync(
                            message.StorageKey, stream, message.ContentType, OssBucketKeys.Learning);
                        _logger.LogInformation("OSS upload SUCCESS: {Url}", fileUrl);

                        await dbContext.Database.ExecuteSqlRawAsync(
                            "UPDATE teacher.TeacherContentItems SET UploadStatus = {0}, PublicUrl = {1} WHERE Id = {2}",
                            UploadStatusReady, fileUrl, message.ContentItemId);

                        _logger.LogInformation(
                            "DB updated: TeacherContentItems.Id={ItemId} → UploadStatus=Ready",
                            message.ContentItemId);
                        _logger.LogInformation("========== TEACHER CONTENT FILE UPLOAD COMPLETE ==========");
                    }
                    else
                    {
                        _logger.LogWarning("Teacher content file message had empty StorageKey — skipping");
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonSerializer.Deserialize<TeacherContentFileUploadMessage>(body);
                        if (message != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
                            await dbContext.Database.ExecuteSqlRawAsync(
                                "UPDATE teacher.TeacherContentItems SET UploadStatus = {0} WHERE Id = {1}",
                                UploadStatusFailed, message.ContentItemId);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Failed to mark teacher content item as failed");
                    }

                    if (ea.Redelivered)
                    {
                        _logger.LogError(ex, "Teacher content upload failed (redelivered — dropping)");
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogError(ex, "Teacher content upload failed (first attempt — requeuing)");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.TeacherContentFileUploadQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "TeacherContentFileUploadConsumer listening on queue: {Queue}",
                _rabbitSettings.TeacherContentFileUploadQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("TeacherContentFileUploadConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeacherContentFileUploadConsumer encountered an error");
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
