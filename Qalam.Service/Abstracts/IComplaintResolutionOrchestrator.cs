using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

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

    Task<ComplaintResolvePreviewDto> GetPreviewAsync(
        int scheduleId,
        int complaintId,
        SessionComplaintResolution resolutionCode,
        decimal? refundAmountOverride,
        int? paymentIdOverride,
        CancellationToken cancellationToken = default);
}
