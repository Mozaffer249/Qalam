using System.ComponentModel.DataAnnotations;
using Qalam.Data.AppMetaData;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Pricing;

/// <summary>
/// Admin-configured student-facing hourly rate per domain × session type.
/// Each row is a version; EffectiveTo = null means the current active rate.
/// Closing a row (setting EffectiveTo) preserves pricing history.
/// </summary>
public class DomainSessionPrice : AuditableEntity
{
    public int Id { get; set; }

    public int DomainId { get; set; }

    [Required, MaxLength(10)]
    public string MarketCode { get; set; } = PricingMarketDefaults.DefaultMarketCode;

    [Required, MaxLength(30)]
    public string SessionTypeCode { get; set; } = default!;

    /// <summary>Hourly rate in the market currency charged to the student.</summary>
    public decimal PricePerHour { get; set; }

    public DateTime EffectiveFrom { get; set; }

    /// <summary>Null while this version is the active rate.</summary>
    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public EducationDomain Domain { get; set; } = null!;

    public PricingMarket Market { get; set; } = null!;
}
