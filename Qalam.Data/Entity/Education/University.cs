using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class University : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameEn { get; set; } = default!;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<College> Colleges { get; set; } = new List<College>();
}
