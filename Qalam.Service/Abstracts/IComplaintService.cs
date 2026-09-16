using Microsoft.AspNetCore.Http;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Complaint;

namespace Qalam.Service.Abstracts;

public interface IComplaintService
{
    Task<(List<ComplaintListItemDto> Items, int Total)> ListAsync(
        ComplaintListFilter filter,
        int pageNumber,
        int pageSize,
        string viewerRole,
        CancellationToken cancellationToken = default);

    Task<ComplaintDetailDto?> GetAsync(
        int complaintId,
        int? viewerUserId,
        string viewerRole,
        int? teacherId = null,
        CancellationToken cancellationToken = default);

    Task<ComplaintCountsDto> GetCountsAsync(
        ComplaintListFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<ComplaintDetailDto> FileAsync(
        int complainantUserId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        IReadOnlyList<IFormFile>? attachments,
        CancellationToken cancellationToken = default);

    Task AssignAsync(int complaintId, int adminUserId, int assignedToUserId, CancellationToken cancellationToken = default);

    Task SetPriorityAsync(
        int complaintId,
        int adminUserId,
        ComplaintPriority priority,
        CancellationToken cancellationToken = default);

    Task RequestInfoAsync(
        int complaintId,
        int adminUserId,
        string target,
        string? notes,
        CancellationToken cancellationToken = default);

    Task AdvanceAsync(
        int complaintId,
        int adminUserId,
        ComplaintStatus? toStatus,
        CancellationToken cancellationToken = default);

    Task CancelAsync(int complaintId, int adminUserId, string notes, CancellationToken cancellationToken = default);

    Task RespondAsComplainantAsync(
        int complaintId,
        int userId,
        string response,
        CancellationToken cancellationToken = default);

    Task RespondAsRespondentAsync(
        int complaintId,
        int userId,
        int? teacherId,
        string response,
        CancellationToken cancellationToken = default);

    Task<ComplaintResolvePreviewDto> GetResolvePreviewAsync(
        int complaintId,
        ComplaintResolution resolutionCode,
        decimal? refundAmount,
        int? paymentId,
        CancellationToken cancellationToken = default);

    Task ResolveAsync(
        int complaintId,
        int adminUserId,
        ResolveComplaintRequest request,
        CancellationToken cancellationToken = default);
}
