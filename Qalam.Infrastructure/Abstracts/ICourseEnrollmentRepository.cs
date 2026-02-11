using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseEnrollmentRepository : IGenericRepositoryAsync<CourseEnrollment>
{
    IQueryable<CourseEnrollment> GetByStudentIdQueryable(int studentId);
}
