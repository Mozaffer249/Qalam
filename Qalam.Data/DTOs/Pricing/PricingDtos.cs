namespace Qalam.Data.DTOs.Pricing;

public class PricingMarketDto
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Currency { get; set; } = default!;
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
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }
}

public class SetDomainSessionPriceDto
{
    public string MarketCode { get; set; } = default!;
    public int DomainId { get; set; }
    public string SessionTypeCode { get; set; } = default!;
    public decimal PricePerHour { get; set; }
    /// <summary>When the new rate takes effect. Defaults to UTC now if omitted.</summary>
    public DateTime? EffectiveFrom { get; set; }
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

public class SetTeacherLevelDto
{
    public int TeacherLevelId { get; set; }
}

public class SetTeacherShareOverrideDto
{
    /// <summary>Null clears the override and reverts to tier default.</summary>
    public decimal? CustomTeacherSharePct { get; set; }
}

public class TeacherLevelUpgradeSuggestionAdminDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string? TeacherName { get; set; }
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
