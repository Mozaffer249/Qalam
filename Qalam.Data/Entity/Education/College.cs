using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

/// <summary>
/// Faculty / College under a university.
/// </summary>
public class College : AuditableEntity
{
    public int Id { get; set; }

    public int UniversityId { get; set; }
    public University University { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(150)]
    public string NameEn { get; set; } = default!;

    [MaxLength(50)]
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Department> Departments { get; set; } = new List<Department>();
}
