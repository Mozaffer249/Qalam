namespace Qalam.Data.DTOs;

public class SubjectDto
{
    public int Id { get; set; }
    public int DomainId { get; set; }
    public string DomainNameAr { get; set; } = default!;
    public string DomainNameEn { get; set; } = default!;
    public int? CurriculumId { get; set; }
    public string? CurriculumNameAr { get; set; }
    public string? CurriculumNameEn { get; set; }
    public int? LevelId { get; set; }
    public string? LevelNameAr { get; set; }
    public string? LevelNameEn { get; set; }
    public int? GradeId { get; set; }
    public string? GradeNameAr { get; set; }
    public string? GradeNameEn { get; set; }
    public int? TermId { get; set; }
    public string? TermNameAr { get; set; }
    public string? TermNameEn { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
