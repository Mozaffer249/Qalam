using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface ILivePresenceWebhookService
{
    /// <summary>
    /// Verify LiveKit Authorization JWT + body checksum, then apply join/leave presence rules.
    /// </summary>
    Task<(bool Ok, int StatusCode, string Message)> HandleLiveKitAsync(
        string rawBody,
        string? authorizationHeader,
        CancellationToken cancellationToken = default);
}

public readonly record struct ParsedLiveIdentity(LivePresenceRole Role, int ParticipantId);
