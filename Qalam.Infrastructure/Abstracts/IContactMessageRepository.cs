using Qalam.Data.Entity.Common;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IContactMessageRepository : IGenericRepositoryAsync<ContactMessage>
{
    Task<ContactMessage?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<PaginatedResult<ContactMessage>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? reason = null,
        string? status = null,
        CancellationToken cancellationToken = default);
}
