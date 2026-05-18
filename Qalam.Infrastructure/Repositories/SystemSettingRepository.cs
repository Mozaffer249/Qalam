using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly ApplicationDBContext _context;

    public SystemSettingRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<SystemSetting> UpsertAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == setting.Key, cancellationToken);

        if (existing == null)
        {
            setting.CreatedAt = DateTime.UtcNow;
            await _context.SystemSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            existing.Value = setting.Value;
            existing.Type = setting.Type;
            existing.IsPublic = setting.IsPublic;
            existing.DescriptionAr = setting.DescriptionAr;
            existing.DescriptionEn = setting.DescriptionEn;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.SystemSettings.Update(existing);
            setting = existing;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return setting;
    }
}
