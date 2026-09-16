using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Complaint;
using AdminPreview = Qalam.Data.DTOs.Admin.ComplaintResolvePreviewDto;

namespace Qalam.Service.Abstracts;

public interface IComplaintResolutionOrchestrator
{
    Task ResolveAsync(
        int scheduleId,
        int complaintId,
        int adminUserId,
        SessionComplaintResolution resolutionCode,
        string? resolutionNotes,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default);

    Task<AdminPreview> GetPreviewAsync(
        int scheduleId,
        int complaintId,
        SessionComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default);

    Task ResolveUnifiedAsync(
        Complaint complaint,
        int adminUserId,
        ResolveComplaintRequest request,
        CancellationToken cancellationToken = default);

    Task<ComplaintResolvePreviewDto> GetPreviewUnifiedAsync(
        Complaint complaint,
        ComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default);
}
