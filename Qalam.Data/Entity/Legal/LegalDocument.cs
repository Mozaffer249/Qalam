using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Legal;

/// <summary>Platform legal document (Privacy Policy, Terms, Refund, Pricing).</summary>
public class LegalDocument : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>Stable lookup key. See <see cref="LegalDocumentCodes"/>.</summary>
    [Required, MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = null!;

    [Required, MaxLength(200)]
    public string TitleEn { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When true, users must accept the published version (consent tracking).</summary>
    public bool RequiresConsent { get; set; }

    public int? CurrentPublishedVersionId { get; set; }

    public LegalDocumentVersion? CurrentPublishedVersion { get; set; }
    public ICollection<LegalDocumentVersion> Versions { get; set; } = new List<LegalDocumentVersion>();
}
