using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IContentUnitRepository : IGenericRepositoryAsync<ContentUnit>
{
    IQueryable<ContentUnit> GetContentUnitsQueryable();
    IQueryable<ContentUnit> GetContentUnitsBySubjectId(int subjectId);
    IQueryable<ContentUnit> GetContentUnitsByTermId(int termId);
    Task<ContentUnit> GetContentUnitWithLessonsAsync(int id);
    Task<int> GetNextOrderIndexAsync(int subjectId, int termId);
    Task UpdateRangeAsync(IEnumerable<ContentUnit> entities);
}
