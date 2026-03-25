using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Qalam.MessagingApi.Services;

public class RabbitMQService : IMessageQueueService, IAsyncDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public RabbitMQService(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            var queues = new[]
            {
                _settings.EmailQueueName,
                _settings.SmsQueueName,
                _settings.PushQueueName,
                _settings.TeacherDocUploadQueueName,
                _settings.ProfilePicUploadQueueName
            };

            foreach (var queue in queues)
            {
                await _channel.QueueDeclareAsync(
                    queue: queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }

            _initialized = true;
            _logger.LogInformation("RabbitMQ connection established. Queues: {Queues}", string.Join(", ", queues));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task PublishAsync<T>(string queueName, T message)
    {
        await EnsureInitializedAsync();

        var messageJson = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = new BasicProperties { Persistent = true };

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async Task QueueEmailAsync(EmailMessage emailMessage)
    {
        try
        {
            emailMessage.QueuedAt = DateTime.UtcNow;
            await PublishAsync(_settings.EmailQueueName, emailMessage);
            _logger.LogInformation("Email queued successfully to: {To}", emailMessage.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email to: {To}", emailMessage.To);
            throw;
        }
    }

    public async Task QueueSmsAsync(SmsMessage smsMessage)
    {
        try
        {
            smsMessage.QueuedAt = DateTime.UtcNow;
            await PublishAsync(_settings.SmsQueueName, smsMessage);
            _logger.LogInformation("SMS queued successfully to: {PhoneNumber}", smsMessage.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue SMS to: {PhoneNumber}", smsMessage.PhoneNumber);
            throw;
        }
    }

    public async Task QueuePushNotificationAsync(PushNotificationMessage pushMessage)
    {
        try
        {
            pushMessage.QueuedAt = DateTime.UtcNow;
            await PublishAsync(_settings.PushQueueName, pushMessage);
            _logger.LogInformation("Push notification queued to: {DeviceToken}", pushMessage.DeviceToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue push notification to: {DeviceToken}", pushMessage.DeviceToken);
            throw;
        }
    }

    public async Task QueueTeacherDocUploadAsync(TeacherDocUploadMessage message)
    {
        try
        {
            message.QueuedAt = DateTime.UtcNow;
            await PublishAsync(_settings.TeacherDocUploadQueueName, message);
            _logger.LogInformation("Teacher doc upload queued: TeacherId={TeacherId}, DocId={DocumentId}",
                message.TeacherId, message.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue teacher doc upload: TeacherId={TeacherId}", message.TeacherId);
            throw;
        }
    }

    public async Task QueueProfilePicUploadAsync(ProfilePicUploadMessage message)
    {
        try
        {
            message.QueuedAt = DateTime.UtcNow;
            await PublishAsync(_settings.ProfilePicUploadQueueName, message);
            _logger.LogInformation("Profile pic upload queued: UserId={UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue profile pic upload: UserId={UserId}", message.UserId);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        _initLock.Dispose();
    }
}
