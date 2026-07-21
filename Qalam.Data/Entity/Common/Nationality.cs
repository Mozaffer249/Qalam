using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Common;

/// <summary>
/// Admin-managed nationality / country lookup (ISO 3166-1 alpha-2).
/// </summary>
public class Nationality : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>ISO 3166-1 alpha-2 code, e.g. SA, EG.</summary>
    [Required, MaxLength(2)]
    public string Code { get; set; } = null!;

    [Required, MaxLength(200)]
    public string NameAr { get; set; } = null!;

    [Required, MaxLength(200)]
    public string NameEn { get; set; } = null!;

    /// <summary>Flag emoji (Unicode regional indicators), e.g. 🇸🇦.</summary>
    [MaxLength(32)]
    public string? FlagEmoji { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
