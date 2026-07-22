using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICollegeRepository : IGenericRepositoryAsync<College>
{
    Task<List<CollegeDto>> GetCollegesDtoAsync(int? universityId = null, bool activeOnly = false, CancellationToken ct = default);
    Task<CollegeDto?> GetCollegeDtoByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<List<FilterOptionDto>> GetCollegesAsOptionsAsync(int universityId, CancellationToken ct = default);
}
