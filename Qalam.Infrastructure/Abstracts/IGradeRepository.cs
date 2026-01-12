using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IGradeRepository : IGenericRepositoryAsync<Grade>
{
    IQueryable<Grade> GetGradesQueryable();
    IQueryable<Grade> GetGradesByLevelId(int levelId);
    IQueryable<Grade> GetGradesByCurriculumId(int curriculumId);
    Task<Grade> GetGradeWithSubjectsAsync(int id);
    Task<bool> IsGradeCodeUniqueAsync(string code, int? excludeId = null);
}
