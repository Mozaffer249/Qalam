using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class PricingMarketRepository : GenericRepositoryAsync<PricingMarket>, IPricingMarketRepository
{
    private readonly ApplicationDBContext _context;
    private readonly DbSet<PricingMarket> _set;

    public PricingMarketRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
        _set = context.Set<PricingMarket>();
    }

    public Task<List<PricingMarket>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);

    public Task<List<PricingMarket>> ListAllAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .OrderByDescending(m => m.IsDefault)
            .ThenBy(m => m.Code)
            .ToListAsync(cancellationToken);

    public Task<PricingMarket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == code, cancellationToken);

    public Task<PricingMarket?> GetByCodeTrackedAsync(string code, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(m => m.Code == code, cancellationToken);

    public Task<PricingMarket?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsDefault && m.IsActive, cancellationToken);

    public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .AnyAsync(m => m.Code == code, cancellationToken);

    public Task<bool> ExistsActiveAsync(string code, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .AnyAsync(m => m.Code == code && m.IsActive, cancellationToken);

    public Task ClearDefaultFlagAsync(CancellationToken cancellationToken = default) =>
        _set.Where(m => m.IsDefault)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsDefault, false).SetProperty(m => m.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

    public Task<bool> HasUserPreferencesAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Set<User>()
            .AsNoTracking()
            .AnyAsync(u => u.PreferredMarketCode == code, cancellationToken);
}
