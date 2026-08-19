using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Pricing;

/// <summary>
/// Immutable pricing breakdown captured at the moment a price is computed for a transaction.
/// </summary>
public class PricingSnapshot : AuditableEntity
{
    public int Id { get; set; }

    public PricingSnapshotContext Context { get; set; }

    public int ContextEntityId { get; set; }

    public int DomainId { get; set; }

    [Required, MaxLength(30)]
    public string SessionTypeCode { get; set; } = default!;

    public int? DomainSessionPriceId { get; set; }

    public decimal PricePerHour { get; set; }

    public int TotalMinutes { get; set; }

    public decimal TotalPrice { get; set; }

    public int TeacherId { get; set; }

    public int? TeacherLevelId { get; set; }

    public decimal TeacherSharePct { get; set; }

    public decimal TeacherEarnings { get; set; }

    public decimal PlatformShare { get; set; }

    public DomainSessionPrice? DomainSessionPrice { get; set; }
}
