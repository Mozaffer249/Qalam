namespace Qalam.Data.DTOs.Legal;

// ── Admin DTOs ──────────────────────────────────────────────────────────────

public class LegalDocumentListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresConsent { get; set; }
    public bool HasArabic { get; set; }
    public bool HasEnglish { get; set; }
    public string? CurrentVersionLabel { get; set; }
    public string? CurrentVersionStatus { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

public class CreateLegalDocumentDto
{
    public string Code { get; set; } = null!;
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool RequiresConsent { get; set; }
}

public class UpdateLegalDocumentDto
{
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresConsent { get; set; }
}

public class LegalDocumentVersionSummaryDto
{
    public int Id { get; set; }
    public int LegalDocumentId { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public string VersionLabel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ChangeNotes { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? PublishedByUserId { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
}

public class LegalDocumentVersionDetailDto : LegalDocumentVersionSummaryDto
{
    public string DocumentCode { get; set; } = null!;
    public string DocumentTitleAr { get; set; } = null!;
    public string DocumentTitleEn { get; set; } = null!;
    public List<LegalDocumentSectionDto> Sections { get; set; } = new();
}

public class LegalDocumentSectionDto
{
    public int Id { get; set; }
    public int LegalDocumentVersionId { get; set; }
    public int? ParentSectionId { get; set; }
    public string AnchorKey { get; set; } = null!;
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; }
    public List<LegalDocumentSectionDto> Children { get; set; } = new();
}

public class CreateLegalDocumentVersionDto
{
    /// <summary>When set, deep-copies sections from this version (restore / fork).</summary>
    public int? SourceVersionId { get; set; }
    public string? ChangeNotes { get; set; }
    public bool IsMajor { get; set; }
}

public class UpdateLegalDocumentVersionDto
{
    public string? ChangeNotes { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class PublishLegalDocumentVersionDto
{
    public bool IsMajor { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? ChangeNotes { get; set; }
}

public class CreateLegalDocumentSectionDto
{
    public int? ParentSectionId { get; set; }
    public string AnchorKey { get; set; } = null!;
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateLegalDocumentSectionDto
{
    public string? AnchorKey { get; set; }
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class ReorderLegalDocumentSectionItemDto
{
    public int Id { get; set; }
    public int? ParentSectionId { get; set; }
    public int DisplayOrder { get; set; }
}

public class ReorderLegalDocumentSectionsDto
{
    public List<ReorderLegalDocumentSectionItemDto> Items { get; set; } = new();
}

// ── Public DTOs ─────────────────────────────────────────────────────────────

public class PublicLegalDocumentSummaryDto
{
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string VersionLabel { get; set; } = null!;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class PublicLegalDocumentDto
{
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string VersionLabel { get; set; } = null!;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<PublicLegalSectionDto> Sections { get; set; } = new();
}

public class PublicLegalSectionDto
{
    public string AnchorKey { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public List<PublicLegalSectionDto> Children { get; set; } = new();
}

// ── Consent DTOs ────────────────────────────────────────────────────────────

public class PendingConsentDocumentDto
{
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int VersionId { get; set; }
    public string VersionLabel { get; set; } = null!;
    public DateTime? EffectiveDate { get; set; }
}

public class AcceptLegalConsentsDto
{
    /// <summary>Document codes to accept (published versions). Empty = accept all pending.</summary>
    public List<string>? DocumentCodes { get; set; }

    /// <summary>e.g. teacher-register, student-register, accept-terms, reaccept.</summary>
    public string? Source { get; set; }
}
