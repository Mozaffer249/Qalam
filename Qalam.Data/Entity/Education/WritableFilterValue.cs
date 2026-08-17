using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class WritableFilterValue : AuditableEntity
{
    public int Id { get; set; }

    public int SlotId { get; set; }
    public WritableFilterSlot Slot { get; set; } = default!;

    [MaxLength(80)]
    public string? Code { get; set; }

    [Required, MaxLength(200)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(200)]
    public string NameEn { get; set; } = default!;

    [Required, MaxLength(200)]
    public string NormalizedText { get; set; } = default!;

    /// <summary>
    /// When set, value is offered only if the selected subject code contains this token
    /// (e.g. "lang.en" for English curricula, "lang.ar-nns" for Arabic NNS).
    /// Null/empty = available for every subject in the slot.
    /// </summary>
    [MaxLength(40)]
    public string? SubjectCodeContains { get; set; }

    public bool IsSeeded { get; set; }

    public bool IsActive { get; set; } = true;
}
