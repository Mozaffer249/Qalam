using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class PricingMarketRepository : GenericRepositoryAsync<PricingMarket>, IPricingMarketRepository
{
    private readonly DbSet<PricingMarket> _set;

    public PricingMarketRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<PricingMarket>();
    }

    public Task<List<PricingMarket>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);

    public Task<PricingMarket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == code, cancellationToken);

    public Task<PricingMarket?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsDefault && m.IsActive, cancellationToken);

    public Task<bool> ExistsActiveAsync(string code, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .AnyAsync(m => m.Code == code && m.IsActive, cancellationToken);
}
