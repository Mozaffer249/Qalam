using Microsoft.Extensions.Logging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AuthDeviceTokenRegistrar : IAuthDeviceTokenRegistrar
{
    private static readonly HashSet<string> AllowedPlatforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "ios", "android", "web"
    };

    private readonly IUserDeviceTokenRepository _deviceTokens;
    private readonly ILogger<AuthDeviceTokenRegistrar> _logger;

    public AuthDeviceTokenRegistrar(
        IUserDeviceTokenRepository deviceTokens,
        ILogger<AuthDeviceTokenRegistrar> logger)
    {
        _deviceTokens = deviceTokens;
        _logger = logger;
    }

    public async Task TryRegisterAsync(
        int userId,
        string? deviceToken,
        string? platform,
        string? appVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            return;

        var normalizedPlatform = string.IsNullOrWhiteSpace(platform)
            ? "android"
            : platform.Trim().ToLowerInvariant();

        if (!AllowedPlatforms.Contains(normalizedPlatform))
        {
            _logger.LogWarning(
                "Skipping device token registration for user {UserId}: invalid platform '{Platform}'",
                userId, platform);
            return;
        }

        try
        {
            await _deviceTokens.UpsertAsync(
                userId,
                deviceToken.Trim(),
                normalizedPlatform,
                string.IsNullOrWhiteSpace(appVersion) ? null : appVersion.Trim(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Device token registration failed during auth for user {UserId}", userId);
        }
    }
}
