using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class UserNotificationPreferencesRepository
    : GenericRepositoryAsync<UserNotificationPreferences>, IUserNotificationPreferencesRepository
{
    private readonly ApplicationDBContext _context;

    public UserNotificationPreferencesRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<UserNotificationPreferences> GetOrCreateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (existing != null)
            return existing;

        var created = new UserNotificationPreferences
        {
            UserId = userId,
            PushEnabled = true,
            EmailDigestEnabled = true,
            SmsAlertsEnabled = false,
            UpdatedAt = DateTime.UtcNow,
        };

        await _context.UserNotificationPreferences.AddAsync(created, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task UpdatePreferencesAsync(
        UserNotificationPreferences entity,
        CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.UserNotificationPreferences.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
