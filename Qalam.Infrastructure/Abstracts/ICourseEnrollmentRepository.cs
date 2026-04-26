using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ICourseEnrollmentRepository : IGenericRepositoryAsync<CourseEnrollment>
{
    IQueryable<CourseEnrollment> GetByStudentIdQueryable(int studentId);
    Task<List<CourseEnrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct);

    /// <summary>
    /// Tracking load with everything the payment + schedule-generation flow needs.
    /// </summary>
    Task<CourseEnrollment?> GetByIdForPaymentAsync(int id, CancellationToken ct);
}
