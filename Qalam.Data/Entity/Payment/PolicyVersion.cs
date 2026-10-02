using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Versioned cancellation &amp; refund policy. A published row is immutable; publishing a new version
/// closes the previous one at the new <see cref="EffectiveFrom"/>. EffectiveTo = null is the open version.
/// </summary>
public class PolicyVersion : AuditableEntity
{
    public int Id { get; set; }

    public int VersionNumber { get; set; }

    public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Draft;

    /// <summary>Set on publish.</summary>
    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    /// <summary>Serialized <see cref="DTOs.Policy.CancellationPolicyRules"/>.</summary>
    [Required]
    public string RulesJson { get; set; } = "{}";

    [MaxLength(500)]
    public string? ChangeNote { get; set; }

    public int? PublishedByUserId { get; set; }

    public DateTime? PublishedAt { get; set; }
}
