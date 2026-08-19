using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherLevelRepository : IGenericRepositoryAsync<TeacherLevel>
{
    Task<TeacherLevel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<TeacherLevel?> GetStarterLevelAsync(CancellationToken cancellationToken = default);
    Task<List<TeacherLevel>> ListOrderedAsync(CancellationToken cancellationToken = default);
    Task<TeacherLevel?> GetNextLevelAsync(int currentOrderIndex, CancellationToken cancellationToken = default);
}
