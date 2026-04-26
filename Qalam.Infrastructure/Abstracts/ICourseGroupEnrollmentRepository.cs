using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseGroupEnrollmentRepository : IGenericRepositoryAsync<CourseGroupEnrollment>
{
    Task<List<CourseGroupEnrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct);

    /// <summary>
    /// Tracking load with everything the per-member payment + schedule-generation flow needs.
    /// </summary>
    Task<CourseGroupEnrollment?> GetByIdForPaymentAsync(int id, CancellationToken ct);
}
