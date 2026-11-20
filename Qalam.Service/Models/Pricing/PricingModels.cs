using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Models.Pricing;

public sealed class PricingEstimateRequest
{
    public int DomainId { get; init; }
    public string SessionTypeCode { get; init; } = default!;
    public string MarketCode { get; init; } = default!;
    public int TotalMinutes { get; init; }
    public int TeacherId { get; init; }
    public DateTime? AsOf { get; init; }

    /// <summary>
    /// When true, the teacher's custom per-hour price flows straight to the
    /// student-facing price and the teacher's own <c>ReflectCustomPriceToStudent</c>
    /// flag is ignored. Targeted OSR sets this because the student is explicitly
    /// requesting this one teacher at their rate — not buying a published course
    /// where the platform brokers a uniform list price.
    /// </summary>
    public bool AlwaysReflectCustomPrice { get; init; }
}

public sealed record PriceEstimate(
    decimal PricePerHour,
    int TotalMinutes,
    decimal TotalPrice,
    decimal TeacherSharePct,
    decimal TeacherEarnings,
    decimal PlatformShare,
    int? DomainSessionPriceId,
    int? TeacherLevelId,
    string MarketCode,
    string Currency,
    bool ReflectCustomPriceToStudent = false,
    decimal? EarningsPricePerHour = null);

/// <summary>
/// What the teacher will earn, using the projected share (custom → level → effective).
/// This is the only money figure teacher-facing screens should show.
/// </summary>
public sealed record TeacherEarningsProjection(
    decimal SharePct,
    decimal EarningsPerHour,
    decimal EarningsTotal);

public sealed class CreatePricingSnapshotRequest
{
    public PricingSnapshotContext Context { get; init; }
    public int ContextEntityId { get; init; }
    public int DomainId { get; init; }
    public string SessionTypeCode { get; init; } = default!;
    public string MarketCode { get; init; } = default!;
    public int TotalMinutes { get; init; }
    public int TeacherId { get; init; }
    public DateTime? AsOf { get; init; }

    /// <inheritdoc cref="PricingEstimateRequest.AlwaysReflectCustomPrice"/>
    public bool AlwaysReflectCustomPrice { get; init; }
}
