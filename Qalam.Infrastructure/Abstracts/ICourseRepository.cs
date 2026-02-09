using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseRepository : IGenericRepositoryAsync<Course>
{
    Task<Course?> GetByIdWithDetailsAsync(int id);
    IQueryable<Course> GetByTeacherIdQueryable(int teacherId);
    Task<bool> HasEnrollmentsAsync(int courseId);
}
