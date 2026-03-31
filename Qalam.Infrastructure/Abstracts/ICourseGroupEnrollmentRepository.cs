using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseGroupEnrollmentRepository : IGenericRepositoryAsync<CourseGroupEnrollment>
{
    Task<List<CourseGroupEnrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct);
}
