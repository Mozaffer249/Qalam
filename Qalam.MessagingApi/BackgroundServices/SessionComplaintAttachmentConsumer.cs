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
public class SessionComplaintAttachmentConsumer : BackgroundService
{
    private readonly ILogger<SessionComplaintAttachmentConsumer> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public SessionComplaintAttachmentConsumer(
        ILogger<SessionComplaintAttachmentConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionComplaintAttachmentConsumer starting...");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitSettings.HostName,
                Port = _rabbitSettings.Port,
                UserName = _rabbitSettings.UserName,
                Password = _rabbitSettings.Password,
                VirtualHost = _rabbitSettings.VirtualHost,
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: _rabbitSettings.SessionComplaintAttachmentUploadQueueName,
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

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process session complaint attachment upload");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.SessionComplaintAttachmentUploadQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "SessionComplaintAttachmentConsumer listening on queue: {Queue}",
                _rabbitSettings.SessionComplaintAttachmentUploadQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("SessionComplaintAttachmentConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionComplaintAttachmentConsumer encountered an error");
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
