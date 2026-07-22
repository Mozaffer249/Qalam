using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IUniversityRepository : IGenericRepositoryAsync<University>
{
    Task<List<UniversityDto>> GetUniversitiesDtoAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<UniversityDto?> GetUniversityDtoByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<List<FilterOptionDto>> GetUniversitiesAsOptionsAsync(CancellationToken ct = default);
}
