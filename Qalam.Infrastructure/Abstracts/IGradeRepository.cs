using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IGradeRepository : IGenericRepositoryAsync<Grade>
{
    IQueryable<Grade> GetGradesQueryable();
    IQueryable<GradeDto> GetGradesDtoQueryable();
    IQueryable<GradeDto> GetGradesDtoByLevelId(int levelId);
    IQueryable<Grade> GetGradesByLevelId(int levelId);
    IQueryable<Grade> GetGradesByCurriculumId(int curriculumId);
    Task<Grade> GetGradeWithSubjectsAsync(int id);
    Task<GradeDto?> GetGradeDtoByIdAsync(int id);
    Task<bool> IsGradeCodeUniqueAsync(string code, int? excludeId = null);
}
