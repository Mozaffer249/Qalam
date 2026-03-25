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

public class ProfilePicUploadConsumer : BackgroundService
{
    private readonly ILogger<ProfilePicUploadConsumer> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public ProfilePicUploadConsumer(
        ILogger<ProfilePicUploadConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProfilePicUploadConsumer starting...");

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
                queue: _rabbitSettings.ProfilePicUploadQueueName,
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
                    var message = JsonSerializer.Deserialize<ProfilePicUploadMessage>(body);

                    if (message != null)
                    {
                        _logger.LogInformation("========== PROFILE PIC UPLOAD ==========");
                        _logger.LogInformation("Received: UserId={UserId}, File={FileName}, Size={Size}bytes",
                            message.UserId, message.FileName, message.FileData.Length);

                        using var scope = _scopeFactory.CreateScope();
                        var wasabiService = scope.ServiceProvider.GetRequiredService<IWasabiStorageService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

                        var fileBytes = Convert.FromBase64String(message.FileData);
                        using var stream = new MemoryStream(fileBytes);
                        _logger.LogInformation("Decoded base64 → {ByteCount} bytes", fileBytes.Length);

                        var extension = Path.GetExtension(message.FileName);
                        var key = $"profiles/{message.UserId}/{Guid.NewGuid()}{extension}";
                        _logger.LogInformation("Uploading to Wasabi key: {Key}", key);

                        var fileUrl = await wasabiService.UploadFileAsync(key, stream, message.ContentType);
                        _logger.LogInformation("Wasabi upload SUCCESS: {Url}", fileUrl);

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

                        _logger.LogInformation("========== PROFILE PIC COMPLETE ==========");
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process profile pic upload");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitSettings.ProfilePicUploadQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("ProfilePicUploadConsumer listening on queue: {Queue}", _rabbitSettings.ProfilePicUploadQueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("ProfilePicUploadConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProfilePicUploadConsumer encountered an error");
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
