using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using System.Text;
using System.Text.Json;

namespace Qalam.Service.BackgroundServices
{
    public class PushConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PushConsumerService> _logger;
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly PushNotificationSettings _pushSettings;
        private IConnection? _connection;
        private IChannel? _channel;
        private static bool _firebaseInitialized = false;
        private static readonly object _lock = new object();

        public PushConsumerService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PushConsumerService> logger,
            IOptions<RabbitMQSettings> rabbitMQSettings,
            IOptions<PushNotificationSettings> pushSettings)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _rabbitMQSettings = rabbitMQSettings.Value;
            _pushSettings = pushSettings.Value;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            if (_firebaseInitialized) return;

            lock (_lock)
            {
                if (_firebaseInitialized) return;

                try
                {
                    if (!string.IsNullOrEmpty(_pushSettings.ServiceAccountKeyPath) &&
                        File.Exists(_pushSettings.ServiceAccountKeyPath))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(_pushSettings.ServiceAccountKeyPath)
                        });
                        _firebaseInitialized = true;
                        _logger.LogInformation("Firebase initialized successfully for Push Consumer");
                    }
                    else
                    {
                        _logger.LogWarning("Firebase service account key not found. Push notifications will not work.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Firebase for Push Consumer");
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Push Consumer Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = _rabbitMQSettings.HostName,
                        Port = _rabbitMQSettings.Port,
                        UserName = _rabbitMQSettings.UserName,
                        Password = _rabbitMQSettings.Password,
                        VirtualHost = _rabbitMQSettings.VirtualHost
                    };

                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}. Retrying in 30 seconds...",
                        _rabbitMQSettings.HostName, _rabbitMQSettings.Port);
                    try { await Task.Delay(30000, stoppingToken); }
                    catch (TaskCanceledException) { return; }
                }
            }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await _channel!.QueueDeclareAsync(
                    queue: _rabbitMQSettings.PushQueueName, durable: true, exclusive: false,
                    autoDelete: false, arguments: null, cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);

                    try
                    {
                        var pushMessage = JsonSerializer.Deserialize<PushNotificationMessage>(messageJson);
                        if (pushMessage != null)
                        {
                            _logger.LogInformation("Processing queued push notification to: {DeviceToken}", pushMessage.DeviceToken);
                            await SendPushNotificationDirectlyAsync(pushMessage);
                            _logger.LogInformation("Successfully processed queued push notification to: {DeviceToken}", pushMessage.DeviceToken);
                        }
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process queued push notification. Message: {Message}", messageJson);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: _rabbitMQSettings.PushQueueName, autoAck: false,
                    consumer: consumer, cancellationToken: stoppingToken);

                _logger.LogInformation("Push Consumer Service started. Listening on queue: {Queue}", _rabbitMQSettings.PushQueueName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Push Consumer Service is shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push Consumer Service encountered an error");
                throw;
            }
        }

        private async Task SendPushNotificationDirectlyAsync(PushNotificationMessage pushMessage)
        {
            if (!_firebaseInitialized)
                throw new InvalidOperationException("Firebase is not initialized");

            var message = new Message()
            {
                Token = pushMessage.DeviceToken,
                Notification = new Notification()
                {
                    Title = pushMessage.Title,
                    Body = pushMessage.Body
                },
                Data = pushMessage.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Push notification sent successfully. Response: {Response}", response);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Push Consumer Service stopping...");
            if (_channel != null) { await _channel.CloseAsync(cancellationToken); await _channel.DisposeAsync(); }
            if (_connection != null) { await _connection.CloseAsync(cancellationToken); await _connection.DisposeAsync(); }
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
