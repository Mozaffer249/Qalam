using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.Entity.Quran;

public class QuranPart
{
    public int Id { get; set; }
    
    [Range(1, 30)]
    public int PartNumber { get; set; }
    
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;
}

