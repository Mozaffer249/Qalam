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
        string marketCode,
        DateTime asOf,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(p => p.Market)
            .Where(p =>
                p.MarketCode == marketCode
                && p.DomainId == domainId
                && p.SessionTypeCode == sessionTypeCode
                && p.IsActive
                && p.EffectiveFrom <= asOf
                && (p.EffectiveTo == null || p.EffectiveTo > asOf))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<DomainSessionPrice?> GetCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        CancellationToken cancellationToken = default) =>
        GetEffectiveRateAsync(domainId, sessionTypeCode, marketCode, DateTime.UtcNow, cancellationToken);

    public Task<List<DomainSessionPrice>> ListHistoryAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(p => p.Domain)
            .Include(p => p.Market)
            .Where(p =>
                p.MarketCode == marketCode
                && p.DomainId == domainId
                && p.SessionTypeCode == sessionTypeCode)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<List<DomainSessionPrice>> ListCurrentRatesAsync(
        string marketCode,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(p => p.Domain)
            .Include(p => p.Market)
            .Where(p => p.MarketCode == marketCode && p.IsActive && p.EffectiveTo == null)
            .OrderBy(p => p.DomainId)
            .ThenBy(p => p.SessionTypeCode)
            .ToListAsync(cancellationToken);

    public async Task CloseCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        DateTime effectiveTo,
        CancellationToken cancellationToken = default)
    {
        var current = await _set
            .Where(p =>
                p.MarketCode == marketCode
                && p.DomainId == domainId
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
