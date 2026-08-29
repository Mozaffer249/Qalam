using Qalam.Data.DTOs.Admin;

namespace Qalam.Service.Abstracts;

public interface IAdminSessionActionService
{
    Task SetAttendanceAsync(
        int scheduleId,
        int adminUserId,
        AdminSetSessionAttendanceRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default);

    Task IssueRefundAsync(
        int scheduleId,
        int adminUserId,
        AdminSessionRefundRequest request,
        CancellationToken cancellationToken = default);

    Task HoldEarningAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default);

    Task ReleaseEarningAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default);

    Task VoidEarningAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default);

    Task WarnTeacherAsync(int scheduleId, int adminUserId, string? notes, CancellationToken cancellationToken = default);

    Task BlockTeacherAsync(int scheduleId, int adminUserId, CancellationToken cancellationToken = default);
}
