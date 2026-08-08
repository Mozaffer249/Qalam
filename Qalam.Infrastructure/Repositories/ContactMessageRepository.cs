using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class ContactMessageRepository : GenericRepositoryAsync<ContactMessage>, IContactMessageRepository
{
    private readonly DbSet<ContactMessage> _set;

    public ContactMessageRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<ContactMessage>();
    }

    public Task<ContactMessage?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<PaginatedResult<ContactMessage>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? reason = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize switch
        {
            < 1 => 20,
            > 50 => 50,
            _ => pageSize
        };

        var query = _set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term)
                || m.Phone.Contains(term)
                || (m.Email != null && m.Email.ToLower().Contains(term))
                || m.Message.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(reason))
            query = query.Where(m => m.Reason == reason);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ContactMessage>(items, totalCount, pageNumber, pageSize);
    }
}
