using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ILessonRepository : IGenericRepositoryAsync<Lesson>
{
    IQueryable<Lesson> GetLessonsQueryable();
    IQueryable<Lesson> GetLessonsByContentUnitId(int contentUnitId, int? quranContentTypeId = null, int? quranLevelId = null);
    IQueryable<Lesson> GetLessonsBySubjectId(int subjectId);
    Task<Lesson> GetLessonWithDetailsAsync(int id);
    Task<int> GetNextOrderIndexAsync(int contentUnitId, int? quranContentTypeId = null, int? quranLevelId = null);
    Task UpdateRangeAsync(IEnumerable<Lesson> entities);
    Task<List<FilterOptionDto>> GetLessonsAsOptionsAsync(int contentUnitId, int? quranContentTypeId = null, int? quranLevelId = null);
}
