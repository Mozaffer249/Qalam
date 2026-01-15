namespace Qalam.Data.DTOs;

public class CurriculumDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Country { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
 
}
