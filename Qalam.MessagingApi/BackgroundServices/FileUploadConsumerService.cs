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

public class FileUploadConsumerService : BackgroundService
{
    private readonly ILogger<FileUploadConsumerService> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public FileUploadConsumerService(
        ILogger<FileUploadConsumerService> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileUploadConsumerService starting...");

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
                queue: _rabbitSettings.FileUploadQueueName,
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
                    var uploadMessage = JsonSerializer.Deserialize<FileUploadMessage>(body);

                    if (uploadMessage != null)
                    {
                        _logger.LogInformation("========== FILE UPLOAD CONSUMER ==========");
                        _logger.LogInformation("Received: Teacher={TeacherId}, Doc={EntityId}, File={FileName}, Type={ContentType}, DataSize={Size}bytes",
                            uploadMessage.TeacherId, uploadMessage.EntityId, uploadMessage.FileName,
                            uploadMessage.ContentType, uploadMessage.FileData.Length);

                        using var scope = _scopeFactory.CreateScope();
                        var wasabiService = scope.ServiceProvider.GetRequiredService<IWasabiStorageService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

                        // Decode base64 to stream
                        var fileBytes = Convert.FromBase64String(uploadMessage.FileData);
                        using var stream = new MemoryStream(fileBytes);
                        _logger.LogInformation("Decoded base64 → {ByteCount} bytes", fileBytes.Length);

                        // Build S3 key: teachers/{teacherId}/{docType}/{guid}.ext
                        var extension = Path.GetExtension(uploadMessage.FileName);
                        var key = $"teachers/{uploadMessage.TeacherId}/{uploadMessage.DocumentType}/{Guid.NewGuid()}{extension}";
                        _logger.LogInformation("Uploading to Wasabi key: {Key}", key);

                        // Upload to Wasabi
                        var fileUrl = await wasabiService.UploadFileAsync(key, stream, uploadMessage.ContentType);
                        _logger.LogInformation("Wasabi upload SUCCESS: {Url}", fileUrl);

                        // Update TeacherDocument.FilePath in the database (skip if documentId is 0 = test)
                        if (uploadMessage.EntityId > 0)
                        {
                            await dbContext.Database.ExecuteSqlRawAsync(
                                "UPDATE TeacherDocuments SET FilePath = {0} WHERE Id = {1}",
                                fileUrl, uploadMessage.EntityId);
                            _logger.LogInformation("DB updated: TeacherDocuments.Id={EntityId} → FilePath={Url}",
                                uploadMessage.EntityId, fileUrl);
                        }
                        else
                        {
                            _logger.LogInformation("DocumentId=0 (test mode) — skipped DB update");
                        }

                        _logger.LogInformation("========== UPLOAD COMPLETE ==========");
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process file upload message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.FileUploadQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("FileUploadConsumerService listening on queue: {Queue}", _rabbitSettings.FileUploadQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("FileUploadConsumerService stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FileUploadConsumerService encountered an error");
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
