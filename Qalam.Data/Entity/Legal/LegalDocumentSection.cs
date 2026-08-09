using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Legal;

/// <summary>Hierarchical section within a legal document version.</summary>
public class LegalDocumentSection : AuditableEntity
{
    public int Id { get; set; }

    public int LegalDocumentVersionId { get; set; }

    public int? ParentSectionId { get; set; }

    /// <summary>Stable TOC anchor within a version (e.g. "privacy-collect").</summary>
    [Required, MaxLength(100)]
    public string AnchorKey { get; set; } = null!;

    [Required, MaxLength(300)]
    public string TitleAr { get; set; } = null!;

    [Required, MaxLength(300)]
    public string TitleEn { get; set; } = null!;

    public string? ContentAr { get; set; }

    public string? ContentEn { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public LegalDocumentVersion LegalDocumentVersion { get; set; } = null!;
    public LegalDocumentSection? ParentSection { get; set; }
    public ICollection<LegalDocumentSection> ChildSections { get; set; } = new List<LegalDocumentSection>();
}
