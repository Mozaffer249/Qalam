using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IDepartmentRepository : IGenericRepositoryAsync<Department>
{
    Task<List<DepartmentDto>> GetDepartmentsDtoAsync(int? collegeId = null, bool activeOnly = false, CancellationToken ct = default);
    Task<DepartmentDto?> GetDepartmentDtoByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<List<FilterOptionDto>> GetDepartmentsAsOptionsAsync(int collegeId, CancellationToken ct = default);
}
