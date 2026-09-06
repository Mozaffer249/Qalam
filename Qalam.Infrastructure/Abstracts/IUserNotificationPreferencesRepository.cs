using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IUserNotificationPreferencesRepository : IGenericRepositoryAsync<UserNotificationPreferences>
{
    Task<UserNotificationPreferences> GetOrCreateAsync(int userId, CancellationToken cancellationToken = default);
    Task UpdatePreferencesAsync(UserNotificationPreferences entity, CancellationToken cancellationToken = default);
}
