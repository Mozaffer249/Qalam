using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IUserDeviceTokenRepository : IGenericRepositoryAsync<UserDeviceToken>
{
    Task UpsertAsync(int userId, string token, string platform, string? appVersion, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int userId, string token, CancellationToken cancellationToken = default);
    Task DeactivateAllForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetActiveTokensForUserAsync(int userId, CancellationToken cancellationToken = default);
}
