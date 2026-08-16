using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class WritableFilterSlot : AuditableEntity
{
    public int Id { get; set; }

    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;

    [Required, MaxLength(80)]
    public string Code { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;

    /// <summary>Start | ParentSubject | Subject | Level</summary>
    [Required, MaxLength(40)]
    public string AfterStep { get; set; } = default!;

    public int OrderIndex { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>When set, the slot is required only if the selected subject code contains this token (e.g. ".other").</summary>
    [MaxLength(40)]
    public string? RequiredWhenSubjectCodeContains { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<WritableFilterValue> Values { get; set; } = new List<WritableFilterValue>();
}
