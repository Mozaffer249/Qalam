using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface INationalityRepository : IGenericRepositoryAsync<Nationality>
{
    Task<List<Nationality>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<List<Nationality>> GetActiveOrderedAsync(CancellationToken cancellationToken = default);
    Task<Nationality?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
