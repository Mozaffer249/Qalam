using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherLevelUpgradeSuggestionRepository : IGenericRepositoryAsync<TeacherLevelUpgradeSuggestion>
{
    Task<TeacherLevelUpgradeSuggestion?> GetPendingForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<List<TeacherLevelUpgradeSuggestion>> ListByStatusAsync(
        TeacherLevelUpgradeSuggestionStatus status,
        CancellationToken cancellationToken = default);
}
