using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.Entity.Teaching;

public class SessionType
{
    public int Id { get; set; }
    
    [Required, MaxLength(30)]
    public string Code { get; set; } = default!; // individual, group
    
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;
    
    [MaxLength(200)]
    public string? DescriptionAr { get; set; }
    
    [MaxLength(200)]
    public string? DescriptionEn { get; set; }
}

