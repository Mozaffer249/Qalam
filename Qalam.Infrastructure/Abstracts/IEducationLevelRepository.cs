using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEducationLevelRepository : IGenericRepositoryAsync<EducationLevel>
{
    IQueryable<EducationLevel> GetLevelsQueryable();
    IQueryable<EducationLevel> GetLevelsByDomainId(int domainId);
    IQueryable<EducationLevel> GetLevelsByCurriculumId(int curriculumId);
    Task<EducationLevel> GetLevelWithGradesAsync(int id);
    Task<bool> IsLevelCodeUniqueAsync(string code, int? excludeId = null);
}
