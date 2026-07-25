using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Live;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

/// <summary>
/// LiveKit Cloud / self-hosted token minting. Swap by registering another ILiveSessionProvider.
/// </summary>
public class LiveKitLiveSessionProvider : ILiveSessionProvider
{
    public const string Name = "LiveKit";

    private readonly LiveSessionSettings _settings;
    private readonly ILogger<LiveKitLiveSessionProvider> _logger;

    public LiveKitLiveSessionProvider(
        IOptions<LiveSessionSettings> settings,
        ILogger<LiveKitLiveSessionProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string ProviderName => Name;

    public Task<LiveSessionAccessDto> CreateAccessAsync(
        LiveSessionAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lk = _settings.LiveKit;
        if (string.IsNullOrWhiteSpace(lk.Url)
            || string.IsNullOrWhiteSpace(lk.ApiKey)
            || string.IsNullOrWhiteSpace(lk.ApiSecret))
        {
            _logger.LogError("LiveKit is not configured (LiveSession:LiveKit Url/ApiKey/ApiSecret).");
            throw new InvalidOperationException(
                "Live session provider is not configured. Set LiveSession:LiveKit settings.");
        }

        var ttl = request.Ttl > TimeSpan.Zero
            ? request.Ttl
            : TimeSpan.FromMinutes(Math.Max(1, lk.TokenTtlMinutes));

        var isTeacher = string.Equals(request.Role, "teacher", StringComparison.OrdinalIgnoreCase);

        var grants = new VideoGrants
        {
            RoomJoin = true,
            Room = request.RoomName,
            CanPublish = true,
            CanSubscribe = true,
            CanPublishData = true,
            RoomAdmin = isTeacher,
        };

        var token = new AccessToken(lk.ApiKey, lk.ApiSecret)
            .WithIdentity(request.Identity)
            .WithName(string.IsNullOrWhiteSpace(request.DisplayName) ? request.Identity : request.DisplayName)
            .WithGrants(grants)
            .WithAttributes(new Dictionary<string, string>
            {
                ["role"] = isTeacher ? "teacher" : "student",
            })
            .WithTtl(ttl);

        var jwt = token.ToJwt();
        var expiresAt = DateTime.UtcNow.Add(ttl);

        return Task.FromResult(new LiveSessionAccessDto
        {
            Provider = Name,
            ServerUrl = lk.Url.Trim(),
            RoomName = request.RoomName,
            Token = jwt,
            Identity = request.Identity,
            Role = isTeacher ? "teacher" : "student",
            ExpiresAt = expiresAt,
        });
    }
}
