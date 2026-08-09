using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationStatusService
{
    Task<TeacherRegistrationStatusResponseDto> GetStatusForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<TeacherAccountStatusResponseDto> GetAccountStatusForTeacherAsync(int teacherId, int userId, CancellationToken cancellationToken = default);
    Task<List<TeacherRegistrationSubmissionStatusDto>> GetChecklistForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch checklist for admin teacher list/export. Loads active requirements once and
    /// submissions for all teacher ids in a single query.
    /// </summary>
    Task<Dictionary<int, List<TeacherRegistrationSubmissionStatusDto>>> GetChecklistsForTeachersAsync(
        IReadOnlyList<int> teacherIds,
        CancellationToken cancellationToken = default);
}
