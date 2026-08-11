using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEnrollmentRepository : IGenericRepositoryAsync<Enrollment>
{
    /// <summary>
    /// Enrollments where any participant matches the student. Used for "my enrollments" listing.
    /// </summary>
    IQueryable<Enrollment> GetByStudentIdQueryable(int studentId);

    /// <summary>
    /// Enrollments where any participant is in <paramref name="studentIds"/>.
    /// </summary>
    IQueryable<Enrollment> GetByStudentIdsQueryable(IReadOnlyCollection<int> studentIds);

    Task<List<Enrollment>> GetExpiredPendingPaymentAsync(DateTime now, CancellationToken ct);

    /// <summary>
    /// Tracking load with everything the payment + schedule-generation flow needs.
    /// </summary>
    Task<Enrollment?> GetByIdForPaymentAsync(int id, CancellationToken ct);

    /// <summary>
    /// No-tracking load with all participants for detail / list views.
    /// </summary>
    Task<Enrollment?> GetByIdWithParticipantsAsync(int id, CancellationToken ct);

    IQueryable<Enrollment> GetTeacherListQueryable(int teacherId);

    IQueryable<Enrollment> GetCourseListQueryable(int courseId);

    Task<Enrollment?> GetByIdForTeacherDetailAsync(int id, CancellationToken ct);

    Task<Enrollment?> GetByIdWithCourseAsync(int id, CancellationToken ct);

    Task<string?> GetSucceededInvoiceNumberAsync(int enrollmentId, CancellationToken ct);

    Task<string?> GetSucceededPaymentProviderAsync(int enrollmentId, CancellationToken ct);
}
