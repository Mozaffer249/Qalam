using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.Entity.Quran;

public class QuranSurah
{
    public int Id { get; set; }
    
    [Range(1, 114)]
    public int SurahNumber { get; set; }
    
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;
    
    public int AyahCount { get; set; }
    
    public int? PartNumber { get; set; } // الجزء الذي تبدأ فيه السورة
}

