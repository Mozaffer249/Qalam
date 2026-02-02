using Qalam.Data.DTOs;
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

    // Filter options
    Task<List<FilterOptionDto>> GetContentUnitsAsOptionsAsync(int subjectId, string? unitTypeCode, List<int>? termIds = null);
    Task<(List<FilterOptionDto> Options, int TotalCount)> GetContentUnitsAsOptionsAsync(int subjectId, string? unitTypeCode, int pageNumber, int pageSize, List<int>? termIds = null);
}
