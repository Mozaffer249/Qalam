using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly PushSettings _pushSettings;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private static bool _firebaseInitialized;
    private static readonly object _lock = new();

    public PushNotificationService(
        IOptions<PushSettings> pushSettings,
        ILogger<PushNotificationService> logger,
        IMessageQueueService messageQueueService)
    {
        _pushSettings = pushSettings.Value;
        _logger = logger;
        _messageQueueService = messageQueueService;
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
                    FirebaseApp.Create(new AppOptions
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

    public async Task SendPushNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, object>? data = null)
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
                await QueuePushAsync(deviceToken, title, body, data);
                break;
            case SendingStrategy.Fallback:
                await SendWithFallbackAsync(deviceToken, title, body, data);
                break;
            default:
                throw new ArgumentException($"Unknown push notification sending strategy: {strategy}");
        }
    }

    private async Task SendDirectAsync(string deviceToken, string title, string body,
        Dictionary<string, object>? data)
    {
        try
        {
            if (!_firebaseInitialized)
                throw new InvalidOperationException("Firebase is not initialized");

            var message = new Message
            {
                Token = deviceToken,
                Notification = new Notification { Title = title, Body = body },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Push notification sent (Direct) to: {DeviceToken}, Response: {Response}",
                deviceToken, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to: {DeviceToken}", deviceToken);
            throw;
        }
    }

    private async Task QueuePushAsync(string deviceToken, string title, string body,
        Dictionary<string, object>? data)
    {
        await _messageQueueService.QueuePushNotificationAsync(new PushNotificationMessage
        {
            DeviceToken = deviceToken,
            Title = title,
            Body = body,
            Data = data
        });
        _logger.LogInformation("Push notification queued for delivery to: {DeviceToken}", deviceToken);
    }

    private async Task SendWithFallbackAsync(string deviceToken, string title, string body,
        Dictionary<string, object>? data)
    {
        try
        {
            await SendDirectAsync(deviceToken, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct push failed to: {DeviceToken}, falling back to queue", deviceToken);
            try
            {
                await QueuePushAsync(deviceToken, title, body, data);
            }
            catch (Exception queueEx)
            {
                _logger.LogError(queueEx, "Failed to queue push to: {DeviceToken}. Notification will be lost.", deviceToken);
                throw;
            }
        }
    }
}
