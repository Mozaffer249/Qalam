using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Admin-defined question shown when a teacher selects subjects in an education domain.
/// </summary>
public class TeacherDomainQuestion : AuditableEntity
{
    public int Id { get; set; }

    public int DomainId { get; set; }

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

    public bool RequiresAdminReview { get; set; }

    public int SortOrder { get; set; }

    public int MinCount { get; set; } = 1;

    public int MaxCount { get; set; } = 1;

    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    [MaxLength(500)]
    public string AllowedExtensionsJson { get; set; } = "[\".pdf\",\".jpg\",\".jpeg\",\".png\"]";

    public int? MaxLength { get; set; }

    public string? OptionsJson { get; set; }

    public TeacherDocumentType? MapsToDocumentType { get; set; }

    public bool IsSystem { get; set; }

    public EducationDomain Domain { get; set; } = null!;

    public ICollection<TeacherDomainQuestionSubmission> Submissions { get; set; } =
        new List<TeacherDomainQuestionSubmission>();
}
