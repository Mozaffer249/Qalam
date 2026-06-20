using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationStatusService
{
    Task<TeacherRegistrationStatusResponseDto> GetStatusForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<TeacherAccountStatusResponseDto> GetAccountStatusForTeacherAsync(int teacherId, int userId, CancellationToken cancellationToken = default);
    Task<List<TeacherRegistrationSubmissionStatusDto>> GetChecklistForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);
}
