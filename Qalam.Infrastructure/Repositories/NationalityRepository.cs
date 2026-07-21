using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class NationalityRepository : GenericRepositoryAsync<Nationality>, INationalityRepository
{
    private readonly DbSet<Nationality> _set;

    public NationalityRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<Nationality>();
    }

    public Task<List<Nationality>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Id)
            .ToListAsync(cancellationToken);

    public Task<List<Nationality>> GetActiveOrderedAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Id)
            .ToListAsync(cancellationToken);

    public Task<Nationality?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(n => n.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var q = _set.AsNoTracking().Where(n => n.Code == code);
        if (excludeId.HasValue)
            q = q.Where(n => n.Id != excludeId.Value);
        return q.AnyAsync(cancellationToken);
    }
}
