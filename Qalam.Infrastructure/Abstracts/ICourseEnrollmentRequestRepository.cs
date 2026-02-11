using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseEnrollmentRequestRepository : IGenericRepositoryAsync<CourseEnrollmentRequest>
{
    IQueryable<CourseEnrollmentRequest> GetByStudentIdQueryable(int studentId);
}
