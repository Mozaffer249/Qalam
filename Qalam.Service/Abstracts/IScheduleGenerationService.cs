using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Abstracts;

public record ScheduleConflict(int SessionNumber, DateOnly Date, int TeacherAvailabilityId, string Reason);

public record ProposedScheduleSlot(
    int SessionNumber,
    DateOnly Date,
    int TeacherAvailabilityId,
    int DurationMinutes,
    string? Title,
    string? Notes);

public record ScheduleGenerationResult(
    List<ProposedScheduleSlot> Slots,
    List<ScheduleConflict> Conflicts,
    bool FitsInWindow);

public interface IScheduleGenerationService
{
    /// <summary>
    /// Pure (no I/O) computation of candidate dates for an enrollment request.
    /// Used at submit-time validation, teacher/student detail views, and right
    /// before persistence at payment-time.
    ///
    /// Algorithm: round-robin through <paramref name="slots"/> starting at
    /// <paramref name="effectiveStart"/>. Blocked exceptions advance one week
    /// (silent, capped at 52 attempts). Existing scheduled rows are emitted as
    /// hard <see cref="ScheduleConflict"/>s. If a candidate would land past
    /// <paramref name="hardEndDate"/>, FitsInWindow is set to false and the
    /// remaining sessions are not produced.
    /// </summary>
    ScheduleGenerationResult Preview(
        Course course,
        CourseEnrollmentRequest request,
        IReadOnlyList<TeacherAvailability> slots,
        IReadOnlyCollection<TeacherAvailabilityException> blockedExceptions,
        IReadOnlySet<(DateOnly Date, int TeacherAvailabilityId)> existingScheduledSlots,
        DateOnly effectiveStart,
        DateOnly? hardEndDate);

    /// <summary>
    /// Uses concrete per-session dates (aligned with the teacher availability calendar API).
    /// </summary>
    ScheduleGenerationResult PreviewExplicit(
        Course course,
        CourseEnrollmentRequest request,
        IReadOnlyList<(DateOnly Date, int TeacherAvailabilityId)> selectionsInSessionOrder,
        IReadOnlyDictionary<int, TeacherAvailability> availabilityById,
        IReadOnlyCollection<TeacherAvailabilityException> blockedExceptions,
        IReadOnlySet<(DateOnly Date, int TeacherAvailabilityId)> existingScheduledSlots,
        DateOnly? hardEndDate);

    /// <summary>
    /// Materialises CourseSchedule entities for an enrollment that just became fully paid.
    /// Caller must attach the returned schedules to the appropriate enrollment navigation
    /// collection and save the DbContext.
    /// </summary>
    List<CourseSchedule> Generate(
        Course course,
        CourseEnrollmentRequest request,
        int? courseEnrollmentId,
        int? courseGroupEnrollmentId,
        DateOnly startDate);
}
