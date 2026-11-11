using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
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
            if (_initialized && _connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized && _connection is { IsOpen: true } && _channel is { IsOpen: true })
                    return;

                await DisposeConnectionAsync();

                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                    TopologyRecoveryEnabled = true
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: _settings.EmailQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.EmailDeadLetterQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.SmsQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.PushQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.TeacherDocUploadQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.OpenSessionRequestAttachmentUploadQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.TeacherContentFileUploadQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.CourseImageUploadQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.SessionComplaintAttachmentUploadQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.LrsQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                await _channel.QueueDeclareAsync(
                    queue: _settings.LrsDeadLetterQueueName,
                    durable: true, exclusive: false, autoDelete: false, arguments: null);

                _initialized = true;
                _logger.LogInformation("RabbitMQ connection established. Queues: {EmailQueue}, {SmsQueue}, {PushQueue}, {TeacherDocQueue}, {ProfilePicQueue}",
                    _settings.EmailQueueName, _settings.SmsQueueName, _settings.PushQueueName,
                    _settings.TeacherDocUploadQueueName, _settings.ProfilePicUploadQueueName);
            }
            catch (Exception ex)
            {
                _initialized = false;
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task DisposeConnectionAsync()
        {
            _initialized = false;

            if (_channel != null)
            {
                try
                {
                    if (_channel.IsOpen)
                        await _channel.CloseAsync();
                }
                catch { /* ignore close races */ }

                try { await _channel.DisposeAsync(); }
                catch { /* ignore */ }

                _channel = null;
            }

            if (_connection != null)
            {
                try
                {
                    if (_connection.IsOpen)
                        await _connection.CloseAsync();
                }
                catch { /* ignore close races */ }

                try { await _connection.DisposeAsync(); }
                catch { /* ignore */ }

                _connection = null;
            }
        }

        private async Task PublishAsync<T>(string queueName, T message)
        {
            await EnsureInitializedAsync();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = new BasicProperties { Persistent = true };

            try
            {
                await _channel!.BasicPublishAsync(
                    exchange: "", routingKey: queueName,
                    mandatory: false, basicProperties: properties, body: body);
            }
            catch (Exception)
            {
                // Connection may have dropped between init and publish — force reconnect once.
                _initialized = false;
                await EnsureInitializedAsync();
                await _channel!.BasicPublishAsync(
                    exchange: "", routingKey: queueName,
                    mandatory: false, basicProperties: properties, body: body);
            }
        }

        public async Task QueueEmailAsync(EmailMessage emailMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailMessage.To) || !MailboxAddress.TryParse(emailMessage.To, out _))
                    throw new ArgumentException($"Invalid email address: '{emailMessage.To}'", nameof(emailMessage));

                if (string.IsNullOrWhiteSpace(emailMessage.MessageId))
                    emailMessage.MessageId = Guid.NewGuid().ToString();

                emailMessage.QueuedAt = DateTime.UtcNow;

                await PublishAsync(_settings.EmailQueueName, emailMessage);

                _logger.LogInformation("Email queued successfully to: {To}, MessageId: {MessageId}",
                    emailMessage.To, emailMessage.MessageId);
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

                _logger.LogInformation("Push notification queued successfully to: {DeviceToken}", pushMessage.DeviceToken);
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

        public async Task QueueOpenSessionRequestAttachmentUploadAsync(OpenSessionRequestAttachmentUploadMessage message)
        {
            try
            {
                message.QueuedAt = DateTime.UtcNow;
                await PublishAsync(_settings.OpenSessionRequestAttachmentUploadQueueName, message);

                _logger.LogInformation(
                    "Open session request attachment upload queued: RequestId={RequestId}, AttachmentId={AttachmentId}",
                    message.OpenSessionRequestId, message.AttachmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to queue open session request attachment upload: RequestId={RequestId}, AttachmentId={AttachmentId}",
                    message.OpenSessionRequestId, message.AttachmentId);
                throw;
            }
        }

        public async Task QueueTeacherContentFileUploadAsync(TeacherContentFileUploadMessage message)
        {
            try
            {
                message.QueuedAt = DateTime.UtcNow;
                await PublishAsync(_settings.TeacherContentFileUploadQueueName, message);

                _logger.LogInformation(
                    "Teacher content file upload queued: TeacherId={TeacherId}, ItemId={ItemId}",
                    message.TeacherId, message.ContentItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to queue teacher content file upload: TeacherId={TeacherId}, ItemId={ItemId}",
                    message.TeacherId, message.ContentItemId);
                throw;
            }
        }

        public async Task QueueCourseImageUploadAsync(CourseImageUploadMessage message)
        {
            try
            {
                message.QueuedAt = DateTime.UtcNow;
                await PublishAsync(_settings.CourseImageUploadQueueName, message);

                _logger.LogInformation(
                    "Course image upload queued: TeacherId={TeacherId}, Key={Key}",
                    message.TeacherId, message.StorageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to queue course image upload: TeacherId={TeacherId}",
                    message.TeacherId);
                throw;
            }
        }

        public async Task QueueSessionComplaintAttachmentUploadAsync(SessionComplaintAttachmentUploadMessage message)
        {
            try
            {
                message.QueuedAt = DateTime.UtcNow;
                await PublishAsync(_settings.SessionComplaintAttachmentUploadQueueName, message);

                _logger.LogInformation(
                    "Session complaint attachment upload queued: ComplaintId={ComplaintId}, AttachmentId={AttachmentId}",
                    message.ComplaintId, message.AttachmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to queue session complaint attachment upload: ComplaintId={ComplaintId}, AttachmentId={AttachmentId}",
                    message.ComplaintId, message.AttachmentId);
                throw;
            }
        }

        public async Task QueueLrsStatementAsync(LrsStatementMessage message)
        {
            try
            {
                message.QueuedAt = DateTime.UtcNow;
                await PublishAsync(_settings.LrsQueueName, message);

                _logger.LogInformation(
                    "LRS statement queued: Verb={Verb}, Actor={ActorName}, Object={ObjectId}",
                    message.Verb, message.ActorName, message.ObjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to queue LRS statement: Verb={Verb}, Object={ObjectId}",
                    message.Verb, message.ObjectId);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeConnectionAsync();
            _initLock?.Dispose();
        }
    }
}
