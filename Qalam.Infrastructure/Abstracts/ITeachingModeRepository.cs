using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeachingModeRepository : IGenericRepositoryAsync<TeachingMode>
{
    IQueryable<TeachingMode> GetTeachingModesQueryable();
    IQueryable<TeachingMode> GetActiveTeachingModesQueryable();
    Task<TeachingMode> GetTeachingModeByCodeAsync(string code);
    Task<bool> IsTeachingModeCodeUniqueAsync(string code, int? excludeId = null);
}
