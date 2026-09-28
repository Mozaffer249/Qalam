using Qalam.Data.Entity.Common.Enums;
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
    /// Serializes payment confirmation for an enrollment (webhook, client confirm and reconciliation
    /// can race). Must be called inside the current transaction; released on commit/rollback.
    /// </summary>
    Task AcquirePaymentConfirmationLockAsync(int enrollmentId, CancellationToken ct);

    /// <summary>
    /// Committed status and non-cancelled schedule count for the enrollment
    /// (reads the database, not the tracked graph).
    /// </summary>
    Task<(EnrollmentStatus Status, int ScheduleCount)> GetCommittedActivationStateAsync(int enrollmentId, CancellationToken ct);

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

    /// <summary>
    /// True when any enrollment for the given students is Active or PendingPayment.
    /// </summary>
    Task<bool> AnyActiveOrPendingPaymentAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken = default);
}
