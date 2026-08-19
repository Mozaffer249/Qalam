using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class DomainSessionPriceRepository : GenericRepositoryAsync<DomainSessionPrice>, IDomainSessionPriceRepository
{
    private readonly DbSet<DomainSessionPrice> _set;

    public DomainSessionPriceRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<DomainSessionPrice>();
    }

    public Task<DomainSessionPrice?> GetEffectiveRateAsync(
        int domainId,
        string sessionTypeCode,
        DateTime asOf,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(p =>
                p.DomainId == domainId
                && p.SessionTypeCode == sessionTypeCode
                && p.IsActive
                && p.EffectiveFrom <= asOf
                && (p.EffectiveTo == null || p.EffectiveTo > asOf))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<DomainSessionPrice?> GetCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        CancellationToken cancellationToken = default) =>
        GetEffectiveRateAsync(domainId, sessionTypeCode, DateTime.UtcNow, cancellationToken);

    public Task<List<DomainSessionPrice>> ListHistoryAsync(
        int domainId,
        string sessionTypeCode,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(p => p.Domain)
            .Where(p => p.DomainId == domainId && p.SessionTypeCode == sessionTypeCode)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<List<DomainSessionPrice>> ListCurrentRatesAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(p => p.Domain)
            .Where(p => p.IsActive && p.EffectiveTo == null)
            .OrderBy(p => p.DomainId)
            .ThenBy(p => p.SessionTypeCode)
            .ToListAsync(cancellationToken);

    public async Task CloseCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        DateTime effectiveTo,
        CancellationToken cancellationToken = default)
    {
        var current = await _set
            .Where(p =>
                p.DomainId == domainId
                && p.SessionTypeCode == sessionTypeCode
                && p.EffectiveTo == null)
            .ToListAsync(cancellationToken);

        foreach (var row in current)
        {
            row.EffectiveTo = effectiveTo;
            row.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(row);
        }
    }
}
