using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Models.Pricing;

public sealed class PricingEstimateRequest
{
    public int DomainId { get; init; }
    public string SessionTypeCode { get; init; } = default!;
    public int TotalMinutes { get; init; }
    public int TeacherId { get; init; }
    public DateTime? AsOf { get; init; }
}

public sealed record PriceEstimate(
    decimal PricePerHour,
    int TotalMinutes,
    decimal TotalPrice,
    decimal TeacherSharePct,
    decimal TeacherEarnings,
    decimal PlatformShare,
    int? DomainSessionPriceId,
    int? TeacherLevelId);

public sealed class CreatePricingSnapshotRequest
{
    public PricingSnapshotContext Context { get; init; }
    public int ContextEntityId { get; init; }
    public int DomainId { get; init; }
    public string SessionTypeCode { get; init; } = default!;
    public int TotalMinutes { get; init; }
    public int TeacherId { get; init; }
    public DateTime? AsOf { get; init; }
}
