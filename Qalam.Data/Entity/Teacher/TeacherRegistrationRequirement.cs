using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Admin-defined catalog item for teacher onboarding (document, bio, location, etc.).
/// </summary>
public class TeacherRegistrationRequirement : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Code { get; set; } = null!;

    [Required, MaxLength(200)]
    public string NameAr { get; set; } = null!;

    [Required, MaxLength(200)]
    public string NameEn { get; set; } = null!;

    [MaxLength(500)]
    public string? DescriptionAr { get; set; }

    [MaxLength(500)]
    public string? DescriptionEn { get; set; }

    public RegistrationRequirementType RequirementType { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsRequired { get; set; } = true;

    public int SortOrder { get; set; }

    public int MinCount { get; set; } = 1;

    public int MaxCount { get; set; } = 1;

    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>JSON array of extensions, e.g. [".pdf",".jpg"]</summary>
    [MaxLength(500)]
    public string AllowedExtensionsJson { get; set; } = "[\".pdf\",\".jpg\",\".jpeg\",\".png\"]";

    public int? MaxLength { get; set; }

    public TeacherDocumentType? MapsToDocumentType { get; set; }

    public bool IsSystem { get; set; }

    public ICollection<TeacherRegistrationSubmission> Submissions { get; set; } =
        new List<TeacherRegistrationSubmission>();
}
