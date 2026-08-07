using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ChatEmailNotifier : IChatEmailNotifier
{
    private readonly IRabbitMQService _rabbitMq;
    private readonly IRateLimitingService _rateLimit;
    private readonly UserManager<User> _userManager;
    private readonly ChatEmailSettings _settings;
    private readonly ILogger<ChatEmailNotifier> _logger;

    public ChatEmailNotifier(
        IRabbitMQService rabbitMq,
        IRateLimitingService rateLimit,
        UserManager<User> userManager,
        IOptions<ChatEmailSettings> settings,
        ILogger<ChatEmailNotifier> logger)
    {
        _rabbitMq = rabbitMq;
        _rateLimit = rateLimit;
        _userManager = userManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task TryNotifyAsync(
        int conversationId,
        int recipientUserId,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (recipientUserId <= 0)
            return;

        try
        {
            var cooldown = Math.Max(0, _settings.CooldownMinutes);
            var key = $"chat-email:{conversationId}:{recipientUserId}";

            if (cooldown > 0)
            {
                var window = TimeSpan.FromMinutes(cooldown);
                if (!await _rateLimit.IsAllowedAsync(key, maxAttempts: 1, window))
                {
                    _logger.LogDebug(
                        "Skipping chat email for conversation {ConversationId} recipient {RecipientUserId} (cooldown {CooldownMinutes}m).",
                        conversationId,
                        recipientUserId,
                        cooldown);
                    return;
                }

                await _rateLimit.IncrementAsync(key, window);
            }

            var user = await _userManager.FindByIdAsync(recipientUserId.ToString());
            if (user?.Email == null)
                return;

            await _rabbitMq.QueueEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = subject,
                Body = body,
                QueuedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to email chat recipient {RecipientUserId} for conversation {ConversationId}.",
                recipientUserId,
                conversationId);
        }
    }
}
