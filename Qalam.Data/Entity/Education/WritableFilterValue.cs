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

    public bool IsSeeded { get; set; }

    public bool IsActive { get; set; } = true;
}
