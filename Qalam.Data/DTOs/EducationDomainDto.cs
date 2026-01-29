namespace Qalam.Data.DTOs;

public class EducationDomainDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime CreatedAt { get; set; }
}
