using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Legal;

/// <summary>Immutable published history row; drafts are mutated until published.</summary>
public class LegalDocumentVersion : AuditableEntity
{
    public int Id { get; set; }

    public int LegalDocumentId { get; set; }

    public int MajorVersion { get; set; } = 1;

    public int MinorVersion { get; set; }

    /// <summary>See <see cref="LegalDocumentStatus"/>.</summary>
    [Required, MaxLength(30)]
    public string Status { get; set; } = LegalDocumentStatus.Draft;

    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int? PublishedByUserId { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public LegalDocument LegalDocument { get; set; } = null!;
    public ICollection<LegalDocumentSection> Sections { get; set; } = new List<LegalDocumentSection>();

    public string VersionLabel => $"{MajorVersion}.{MinorVersion}";
}
