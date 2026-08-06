using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Services.Interfaces;

public interface IEmailSuppressionService
{
    Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default);

    Task SuppressAsync(
        string email,
        EmailSuppressionReason reason,
        EmailSuppressionSource source,
        string? diagnostic = null,
        CancellationToken cancellationToken = default);
}
