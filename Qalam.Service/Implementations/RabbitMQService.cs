using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using System.Text;
using System.Text.Json;

namespace Qalam.Service.Implementations
{
    public class RabbitMQService : IRabbitMQService, IAsyncDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;

        public RabbitMQService(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

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

                await _channel.QueueDeclareAsync(
                    queue: _settings.EmailQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.SmsQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.PushQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.FileUploadQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                _initialized = true;
                _logger.LogInformation("RabbitMQ connection established. Queues: {EmailQueue}, {SmsQueue}, {PushQueue}, {FileUploadQueue}",
                    _settings.EmailQueueName, _settings.SmsQueueName, _settings.PushQueueName, _settings.FileUploadQueueName);
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

        public async Task QueueEmailAsync(EmailMessage emailMessage)
        {
            try
            {
                await EnsureInitializedAsync();
                emailMessage.QueuedAt = DateTime.UtcNow;

                var messageJson = JsonSerializer.Serialize(emailMessage);
                var body = Encoding.UTF8.GetBytes(messageJson);
                var properties = new BasicProperties { Persistent = true };

                await _channel!.BasicPublishAsync(
                    exchange: "", routingKey: _settings.EmailQueueName,
                    mandatory: false, basicProperties: properties, body: body);

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
                await EnsureInitializedAsync();
                smsMessage.QueuedAt = DateTime.UtcNow;

                var messageJson = JsonSerializer.Serialize(smsMessage);
                var body = Encoding.UTF8.GetBytes(messageJson);
                var properties = new BasicProperties { Persistent = true };

                await _channel!.BasicPublishAsync(
                    exchange: "", routingKey: _settings.SmsQueueName,
                    mandatory: false, basicProperties: properties, body: body);

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
                await EnsureInitializedAsync();
                pushMessage.QueuedAt = DateTime.UtcNow;

                var messageJson = JsonSerializer.Serialize(pushMessage);
                var body = Encoding.UTF8.GetBytes(messageJson);
                var properties = new BasicProperties { Persistent = true };

                await _channel!.BasicPublishAsync(
                    exchange: "", routingKey: _settings.PushQueueName,
                    mandatory: false, basicProperties: properties, body: body);

                _logger.LogInformation("Push notification queued successfully to: {DeviceToken}", pushMessage.DeviceToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue push notification to: {DeviceToken}", pushMessage.DeviceToken);
                throw;
            }
        }

        public async Task QueueFileUploadAsync(FileUploadMessage fileUploadMessage)
        {
            try
            {
                await EnsureInitializedAsync();
                fileUploadMessage.QueuedAt = DateTime.UtcNow;

                var messageJson = JsonSerializer.Serialize(fileUploadMessage);
                var body = Encoding.UTF8.GetBytes(messageJson);
                var properties = new BasicProperties { Persistent = true };

                await _channel!.BasicPublishAsync(
                    exchange: "", routingKey: _settings.FileUploadQueueName,
                    mandatory: false, basicProperties: properties, body: body);

                _logger.LogInformation("File upload queued for teacher: {TeacherId}, document: {EntityId}",
                    fileUploadMessage.TeacherId, fileUploadMessage.EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue file upload for teacher: {TeacherId}", fileUploadMessage.TeacherId);
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
            _initLock?.Dispose();
        }
    }
}
