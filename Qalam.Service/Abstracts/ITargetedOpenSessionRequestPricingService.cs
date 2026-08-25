using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Entity.Pricing;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Freezes directed-OSR price at create/publish and maps frozen quotes for teacher UI / offers.
/// </summary>
public interface ITargetedOpenSessionRequestPricingService
{
    /// <summary>
    /// When the request is targeted and has no snapshot yet, compute and attach a frozen quote.
    /// No-op for broadcast or already-frozen requests.
    /// </summary>
    Task FreezeIfNeededAsync(
        OpenSessionRequest request,
        int marketUserId,
        CancellationToken cancellationToken = default);

    PricingEstimateDto ToEstimateDto(PricingSnapshot snapshot);
}
