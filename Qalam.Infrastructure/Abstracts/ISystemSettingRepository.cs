using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Abstracts;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemSetting> UpsertAsync(SystemSetting setting, CancellationToken cancellationToken = default);
}
