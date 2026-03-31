using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseEnrollmentRequestRepository : IGenericRepositoryAsync<CourseEnrollmentRequest>
{
    IQueryable<CourseEnrollmentRequest> GetByUserIdQueryable(int userId);
    IQueryable<CourseEnrollmentRequest> GetByCourseIdQueryable(int courseId);
    IQueryable<CourseRequestGroupMember> GetPendingInvitationsForStudentsQueryable(List<int> studentIds);
}
