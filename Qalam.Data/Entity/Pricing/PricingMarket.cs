using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Pricing;

/// <summary>
/// Admin-configured pricing market (country/region) with its own currency and rate table.
/// </summary>
public class PricingMarket : AuditableEntity
{
    [Key, MaxLength(10)]
    public string Code { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(3)]
    public string Currency { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    public ICollection<DomainSessionPrice> DomainSessionPrices { get; set; } = new List<DomainSessionPrice>();
}
