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
public class CourseImageUploadConsumer : BackgroundService
{
    private readonly ILogger<CourseImageUploadConsumer> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public CourseImageUploadConsumer(
        ILogger<CourseImageUploadConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CourseImageUploadConsumer starting...");

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
                queue: _rabbitSettings.CourseImageUploadQueueName,
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

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process course image upload");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.CourseImageUploadQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "CourseImageUploadConsumer listening on queue: {Queue}",
                _rabbitSettings.CourseImageUploadQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CourseImageUploadConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourseImageUploadConsumer encountered an error");
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
