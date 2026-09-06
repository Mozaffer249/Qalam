using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class UserDeviceTokenRepository
    : GenericRepositoryAsync<UserDeviceToken>, IUserDeviceTokenRepository
{
    private readonly ApplicationDBContext _context;

    public UserDeviceTokenRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task UpsertAsync(
        int userId,
        string token,
        string platform,
        string? appVersion,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var existing = await _context.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (existing != null)
        {
            existing.UserId = userId;
            existing.Platform = platform;
            existing.AppVersion = appVersion;
            existing.LastSeenAt = now;
            existing.IsActive = true;
            _context.UserDeviceTokens.Update(existing);
        }
        else
        {
            await _context.UserDeviceTokens.AddAsync(new UserDeviceToken
            {
                UserId = userId,
                Token = token,
                Platform = platform,
                AppVersion = appVersion,
                LastSeenAt = now,
                IsActive = true,
                CreatedAt = now,
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeactivateAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token, cancellationToken);

        if (existing == null)
            return false;

        existing.IsActive = false;
        existing.LastSeenAt = DateTime.UtcNow;
        _context.UserDeviceTokens.Update(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DeactivateAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.UserDeviceTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.IsActive = false;
            token.LastSeenAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<string>> GetActiveTokensForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.IsActive)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);
    }
}
