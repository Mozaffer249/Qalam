using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherRegistrationRequirementRepository : IGenericRepositoryAsync<TeacherRegistrationRequirement>
{
    Task<List<TeacherRegistrationRequirement>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<List<TeacherRegistrationRequirement>> GetActiveOrderedAsync(CancellationToken cancellationToken = default);
    Task<TeacherRegistrationRequirement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasSubmissionsAsync(int requirementId, CancellationToken cancellationToken = default);
}
