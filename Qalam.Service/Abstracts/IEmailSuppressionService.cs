using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface IEmailSuppressionService
{
    Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default);

    Task SuppressAsync(
        string email,
        EmailSuppressionReason reason,
        EmailSuppressionSource source,
        string? diagnostic = null,
        CancellationToken cancellationToken = default);

    Task<int> SeedAsync(
        IEnumerable<string> emails,
        EmailSuppressionReason reason,
        EmailSuppressionSource source,
        string? diagnostic = null,
        CancellationToken cancellationToken = default);
}
