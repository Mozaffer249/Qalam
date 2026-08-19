using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// Commission tier for teachers. All new teachers start at the lowest OrderIndex level.
/// Higher tiers grant a larger teacher share (platform share decreases).
/// </summary>
public class TeacherLevel : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Code { get; set; } = default!;

    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;

    public int OrderIndex { get; set; }

    /// <summary>Teacher share of the student price (0–100). Platform share = 100 − TeacherSharePct.</summary>
    public decimal TeacherSharePct { get; set; }

    public bool IsActive { get; set; } = true;
}
