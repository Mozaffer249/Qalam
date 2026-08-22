namespace Qalam.Data.DTOs.Pricing;

public class PricingMarketDto
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Currency { get; set; } = default!;
}

public class PricingMarketAdminDto
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}

public class CreatePricingMarketDto
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public bool IsDefault { get; set; }
    /// <summary>Units of market currency per 1 SAR. Defaults to 1.0.</summary>
    public decimal? ExchangeRateFromBase { get; set; }
}

public class UpdatePricingMarketDto
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class MyPricingMarketDto
{
    public string MarketCode { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string? PreferredMarketCode { get; set; }
}

public class SetMyPricingMarketDto
{
    /// <summary>Null clears preference and restores auto-resolve.</summary>
    public string? MarketCode { get; set; }
}

public class DomainSessionPriceAdminDto
{
    public int Id { get; set; }
    public string MarketCode { get; set; } = default!;
    public string? Currency { get; set; }
    public int DomainId { get; set; }
    public string? DomainCode { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public string SessionTypeCode { get; set; } = default!;
    public decimal PricePerHour { get; set; }
    public decimal? BasePricePerHour { get; set; }
    public bool IsDerived { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }
}

public class SetDomainSessionPriceDto
{
    /// <summary>Ignored — base rates are always stored in SAR (sa market).</summary>
    public string? MarketCode { get; set; }
    public int DomainId { get; set; }
    public string SessionTypeCode { get; set; } = default!;
    public decimal PricePerHour { get; set; }
    /// <summary>When the new rate takes effect. Defaults to UTC now if omitted.</summary>
    public DateTime? EffectiveFrom { get; set; }
}

public class PricingExchangeRateAdminDto
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public decimal ExchangeRateFromBase { get; set; }
    public bool IsActive { get; set; }
}

public class UpdatePricingExchangeRateDto
{
    public decimal ExchangeRateFromBase { get; set; }
}

public class TeacherLevelTierAdminDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
    public decimal TeacherSharePct { get; set; }
    public bool IsActive { get; set; }
}

public class SetTeacherLevelTierDto
{
    public decimal TeacherSharePct { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateTeacherLevelTierDto
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public decimal TeacherSharePct { get; set; }
    public int? OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SetTeacherLevelDto
{
    /// <summary>Required — level is assigned per educational domain.</summary>
    public int DomainId { get; set; }

    public int TeacherLevelId { get; set; }
}

public class SetTeacherShareOverrideDto
{
    /// <summary>Required — share override is per educational domain.</summary>
    public int DomainId { get; set; }

    /// <summary>Null clears the override and reverts to tier default.</summary>
    public decimal? CustomTeacherSharePct { get; set; }
}

public class SetTeacherDomainPricingDto
{
    public int DomainId { get; set; }
    public int? TeacherLevelId { get; set; }
    public decimal? CustomTeacherSharePct { get; set; }
    /// <summary>Optional teacher hourly rate in SAR. Null clears the override.</summary>
    public decimal? CustomPricePerHour { get; set; }
    /// <summary>When true and custom price is set, student pays the teacher rate.</summary>
    public bool ReflectCustomPriceToStudent { get; set; }
}

public class TeacherDomainPricingAdminDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int DomainId { get; set; }
    public string? DomainCode { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public int? TeacherLevelId { get; set; }
    public string? TeacherLevelCode { get; set; }
    public decimal? LevelSharePct { get; set; }
    public decimal? CustomTeacherSharePct { get; set; }
    public decimal? CustomPricePerHour { get; set; }
    public bool ReflectCustomPriceToStudent { get; set; }
    public bool HasCompletedInterviewSession { get; set; }
}

public class TeacherLevelUpgradeSuggestionAdminDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int DomainId { get; set; }
    public string? DomainCode { get; set; }
    public string? DomainNameEn { get; set; }
    public string? DomainNameAr { get; set; }
    public int CurrentLevelId { get; set; }
    public string? CurrentLevelCode { get; set; }
    public int SuggestedLevelId { get; set; }
    public string? SuggestedLevelCode { get; set; }
    public decimal AvgRating { get; set; }
    public int CompletedSessions { get; set; }
    public decimal AttendanceRate { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class ReviewLevelUpgradeSuggestionDto
{
    public string? ReviewNotes { get; set; }
}

public class BackfillStarterTeacherLevelsResultDto
{
    public int UpdatedCount { get; set; }
}

public class CourseHourlyRatePreviewDto
{
    public decimal PricePerHour { get; set; }
    public string Currency { get; set; } = default!;
    public string MarketCode { get; set; } = default!;
    public int? TotalMinutes { get; set; }
    public decimal? EstimatedPackageTotal { get; set; }
}

public class PricingEstimateDto
{
    public decimal PricePerHour { get; set; }
    public string Currency { get; set; } = default!;
    public string MarketCode { get; set; } = default!;
    public int TotalMinutes { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TeacherSharePct { get; set; }
    public decimal TeacherEarnings { get; set; }
    public decimal PlatformShare { get; set; }
}

public class PricingSnapshotDto
{
    public int Id { get; set; }
    public decimal PricePerHour { get; set; }
    public string Currency { get; set; } = default!;
    public string MarketCode { get; set; } = default!;
    public int TotalMinutes { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TeacherSharePct { get; set; }
    public decimal TeacherEarnings { get; set; }
    public decimal PlatformShare { get; set; }
}

public class FreeSessionPolicyStatsDto
{
    public int TeachersPendingInterview { get; set; }
    public int TeachersInterviewCompleted { get; set; }
    public int StudentsUsedFreeTrial { get; set; }
    public int StudentsEligibleForFreeTrial { get; set; }
}
