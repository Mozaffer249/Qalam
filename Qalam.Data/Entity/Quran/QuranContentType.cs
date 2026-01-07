using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Quran;

public class QuranContentType : AuditableEntity
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;
    
    [Required, MaxLength(30)]
    public string Code { get; set; } = default!; // memorization, recitation, tajweed
    
    public bool IsActive { get; set; } = true;
}

