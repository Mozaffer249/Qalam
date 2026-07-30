using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ISessionPresenceService
{
    /// <summary>
    /// Teacher opens the session (CTA / future stream). Marks teacher Present and starts InProgress when allowed.
    /// Rejects joins before session start or after end / terminal statuses.
    /// </summary>
    Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> JoinAsTeacherAsync(
        int userId,
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Teacher leaves the live room. Clears TeacherInRoom and appends a Left presence event.
    /// </summary>
    Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> LeaveAsTeacherAsync(
        int userId,
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Student opens the session. Upserts SessionAttendance Present + JoinedAt.
    /// </summary>
    Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> JoinAsStudentAsync(
        int userId,
        int courseScheduleId,
        CancellationToken cancellationToken = default);
}

public interface ISessionReviewService
{
    Task<(bool Ok, string Message, bool Forbidden, bool NotFound)> SubmitStudentReviewAsync(
        int userId,
        int courseScheduleId,
        int rating,
        string? feedback,
        CancellationToken cancellationToken = default);

    Task<List<SessionReviewDto>> GetReviewsForSessionAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Any student→teacher review for this schedule+student (approved or pending).
    /// </summary>
    Task<SessionReviewDto?> GetStudentToTeacherReviewAsync(
        int courseScheduleId,
        int studentId,
        CancellationToken cancellationToken = default);
}
