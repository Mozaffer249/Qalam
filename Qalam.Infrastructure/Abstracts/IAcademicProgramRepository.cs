using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IAcademicProgramRepository : IGenericRepositoryAsync<AcademicProgram>
{
    Task<List<AcademicProgramDto>> GetProgramsDtoAsync(int? departmentId = null, bool activeOnly = false, CancellationToken ct = default);
    Task<AcademicProgramDto?> GetProgramDtoByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<List<FilterOptionDto>> GetProgramsAsOptionsAsync(int departmentId, CancellationToken ct = default);
}
