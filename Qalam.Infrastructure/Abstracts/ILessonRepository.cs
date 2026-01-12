using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ILessonRepository : IGenericRepositoryAsync<Lesson>
{
    IQueryable<Lesson> GetLessonsQueryable();
    IQueryable<Lesson> GetLessonsByContentUnitId(int contentUnitId);
    IQueryable<Lesson> GetLessonsBySubjectId(int subjectId);
    Task<Lesson> GetLessonWithDetailsAsync(int id);
    Task<int> GetNextOrderIndexAsync(int contentUnitId);
    Task UpdateRangeAsync(IEnumerable<Lesson> entities);
}
