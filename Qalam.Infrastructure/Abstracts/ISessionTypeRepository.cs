using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ISessionTypeRepository : IGenericRepositoryAsync<SessionType>
{
    IQueryable<SessionType> GetSessionTypesQueryable();
    IQueryable<SessionType> GetActiveSessionTypesQueryable();
    Task<SessionType> GetSessionTypeByCodeAsync(string code);
    Task<bool> IsSessionTypeCodeUniqueAsync(string code, int? excludeId = null);
}
