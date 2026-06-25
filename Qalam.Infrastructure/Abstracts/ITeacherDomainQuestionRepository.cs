using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDomainQuestionRepository : IGenericRepositoryAsync<TeacherDomainQuestion>
{
    Task<List<TeacherDomainQuestion>> GetAllOrderedAsync(int? domainId = null, CancellationToken cancellationToken = default);
    Task<List<TeacherDomainQuestion>> GetActiveByDomainIdsAsync(IReadOnlyCollection<int> domainIds, CancellationToken cancellationToken = default);
    Task<List<TeacherDomainQuestion>> GetActiveByDomainIdAsync(int domainId, CancellationToken cancellationToken = default);
    Task<TeacherDomainQuestion?> GetByIdWithDomainAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsInDomainAsync(int domainId, string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasSubmissionsAsync(int questionId, CancellationToken cancellationToken = default);
}
