namespace Qalam.Service.Abstracts;

public interface IEnrollmentCompletionService
{
    /// <summary>
    /// Marks enrollment Completed when last session is done. Idempotent.
    /// </summary>
    Task TryCompleteEnrollmentIfFinishedAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sweep Active enrollments that have no open schedules and at least one Completed.
    /// </summary>
    Task<int> SweepFinishedEnrollmentsAsync(CancellationToken cancellationToken = default);
}
