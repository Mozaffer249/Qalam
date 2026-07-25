using Qalam.Data.DTOs.Live;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Domain gate for live session access: authz, join window, presence, then provider token.
/// </summary>
public interface ILiveSessionAccessService
{
    Task<(bool Ok, string Message, bool Forbidden, bool NotFound, bool Unavailable, LiveSessionAccessDto? Access)>
        GetTeacherAccessAsync(int userId, int courseScheduleId, CancellationToken cancellationToken = default);

    Task<(bool Ok, string Message, bool Forbidden, bool NotFound, bool Unavailable, LiveSessionAccessDto? Access)>
        GetStudentAccessAsync(int userId, int courseScheduleId, CancellationToken cancellationToken = default);
}
