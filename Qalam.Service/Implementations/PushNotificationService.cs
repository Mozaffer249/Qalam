using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly PushNotificationSettings _pushSettings;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly IRabbitMQService _rabbitMQService;
        private static bool _firebaseInitialized = false;
        private static readonly object _lock = new object();

        public PushNotificationService(
            IOptions<PushNotificationSettings> pushSettings,
            ILogger<PushNotificationService> logger,
            IRabbitMQService rabbitMQService)
        {
            _pushSettings = pushSettings.Value;
            _logger = logger;
            _rabbitMQService = rabbitMQService;
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
                        _logger.LogInformation("Firebase initialized successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Firebase service account key not found. Push notifications will not work.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Firebase");
                }
            }
        }

        public async Task SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, object>? data = null)
        {
            await SendPushNotificationAsync(deviceToken, title, body, SendingStrategy.Fallback, data);
        }

        public async Task SendPushNotificationAsync(string deviceToken, string title, string body,
            SendingStrategy strategy, Dictionary<string, object>? data = null)
        {
            switch (strategy)
            {
                case SendingStrategy.Direct:
                    await SendDirectAsync(deviceToken, title, body, data);
                    break;
                case SendingStrategy.Queued:
                    await QueuePushNotificationAsync(deviceToken, title, body, data);
                    break;
                case SendingStrategy.Fallback:
                    await SendWithFallbackAsync(deviceToken, title, body, data);
                    break;
                default:
                    throw new ArgumentException($"Unknown push notification sending strategy: {strategy}");
            }
        }

        private async Task SendDirectAsync(string deviceToken, string title, string body, Dictionary<string, object>? data)
        {
            try
            {
                if (!_firebaseInitialized)
                    throw new InvalidOperationException("Firebase is not initialized");

                var message = new Message()
                {
                    Token = deviceToken,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Push notification sent successfully (Direct) to: {DeviceToken}, Response: {Response}",
                    deviceToken, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to: {DeviceToken}", deviceToken);
                throw;
            }
        }

        private async Task QueuePushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, object>? data)
        {
            await _rabbitMQService.QueuePushNotificationAsync(new PushNotificationMessage
            {
                DeviceToken = deviceToken,
                Title = title,
                Body = body,
                Data = data
            });
            _logger.LogInformation("Push notification queued for delivery to: {DeviceToken}", deviceToken);
        }

        private async Task SendWithFallbackAsync(string deviceToken, string title, string body, Dictionary<string, object>? data)
        {
            try
            {
                await SendDirectAsync(deviceToken, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to: {DeviceToken}. Error: {Message}",
                    deviceToken, ex.Message);
                try
                {
                    await QueuePushNotificationAsync(deviceToken, title, body, data);
                    _logger.LogInformation("Push notification queued for later delivery (Fallback) to: {DeviceToken}", deviceToken);
                }
                catch (Exception queueEx)
                {
                    _logger.LogError(queueEx, "Failed to queue push notification to: {DeviceToken}. Notification will be lost.", deviceToken);
                    throw;
                }
            }
        }
    }
}
