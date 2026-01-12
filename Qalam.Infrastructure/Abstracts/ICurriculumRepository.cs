using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICurriculumRepository : IGenericRepositoryAsync<Curriculum>
{
    IQueryable<Curriculum> GetCurriculumsQueryable();
    IQueryable<Curriculum> GetActiveCurriculumsQueryable();
    Task<Curriculum> GetCurriculumWithLevelsAsync(int id);
    Task<Curriculum> GetCurriculumByCodeAsync(string code);
    Task<bool> IsCurriculumCodeUniqueAsync(string code, int? excludeId = null);
}
