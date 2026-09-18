namespace Qalam.Data.DTOs.Student;

/// <summary>
/// Slim education domain payload for student Home / Domains / filter wizard.
/// Active-only lists; no teacher question enrichment.
/// </summary>
public class StudentEducationDomainDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
}
