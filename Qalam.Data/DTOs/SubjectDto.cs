namespace Qalam.Data.DTOs;

public class SubjectDto
{
    public int Id { get; set; }
    public int DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? TermId { get; set; }
    public string? Code { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
