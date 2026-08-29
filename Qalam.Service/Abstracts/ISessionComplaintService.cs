using Microsoft.AspNetCore.Http;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface ISessionComplaintService
{
    Task<bool> HasBlockingComplaintAsync(int courseScheduleId, CancellationToken cancellationToken = default);

    Task<SessionComplaint> FileComplaintAsync(
        int courseScheduleId,
        int studentId,
        int userId,
        SessionComplaintReason reasonCode,
        string description,
        IReadOnlyList<IFormFile>? attachments,
        CancellationToken cancellationToken = default);

    Task<SessionComplaintDetailDto?> GetComplaintAsync(
        int complaintId,
        int? studentId,
        CancellationToken cancellationToken = default);

    Task<List<SessionComplaint>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task AssignAsync(
        int complaintId,
        int adminUserId,
        int assignedToUserId,
        CancellationToken cancellationToken = default);

    Task RequestTeacherResponseAsync(
        int complaintId,
        int adminUserId,
        CancellationToken cancellationToken = default);

    Task ResolveAsync(
        int scheduleId,
        int complaintId,
        int adminUserId,
        SessionComplaintResolution resolutionCode,
        string? resolutionNotes,
        decimal? refundAmount,
        int? paymentId,
        CancellationToken cancellationToken = default);

    Task<ComplaintResolvePreviewDto> GetResolvePreviewAsync(
        int scheduleId,
        int complaintId,
        SessionComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default);

    Task RespondAsTeacherAsync(
        int complaintId,
        int teacherId,
        string response,
        CancellationToken cancellationToken = default);

    Task HoldEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default);

    Task ReleaseEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default);

    Task VoidEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default);
}
