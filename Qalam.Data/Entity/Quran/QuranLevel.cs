using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Quran;

public class QuranLevel : AuditableEntity
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;
    
    public int OrderIndex { get; set; }
    
    [MaxLength(300)]
    public string? DescriptionAr { get; set; }
    
    [MaxLength(300)]
    public string? DescriptionEn { get; set; }
    
    public bool IsActive { get; set; } = true;
}

