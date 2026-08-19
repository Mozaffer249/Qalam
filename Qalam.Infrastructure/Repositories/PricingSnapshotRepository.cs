using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class PricingSnapshotRepository : GenericRepositoryAsync<PricingSnapshot>, IPricingSnapshotRepository
{
    private readonly DbSet<PricingSnapshot> _set;

    public PricingSnapshotRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<PricingSnapshot>();
    }

    public Task<PricingSnapshot?> GetByContextAsync(
        PricingSnapshotContext context,
        int contextEntityId,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Context == context && s.ContextEntityId == contextEntityId,
                cancellationToken);
}
