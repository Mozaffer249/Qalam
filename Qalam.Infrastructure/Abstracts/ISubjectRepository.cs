using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ISubjectRepository : IGenericRepositoryAsync<Subject>
{
    // IQueryable methods for flexible querying
    IQueryable<Subject> GetSubjectsQueryable();
    IQueryable<Subject> GetSubjectsByDomainId(int domainId);
    IQueryable<Subject> GetSubjectsByCurriculumId(int curriculumId);
    IQueryable<Subject> GetSubjectsByGradeId(int gradeId);
    IQueryable<Subject> GetSubjectsByLevelId(int levelId);
    IQueryable<Subject> GetSubjectsByTermId(int termId);
    IQueryable<Subject> GetActiveSubjectsQueryable();
    
    // Specific operations
    Task<Subject> GetSubjectWithDetailsAsync(int id);
    Task<List<Subject>> GetSubjectsWithContentUnitsAsync(int gradeId);
}
