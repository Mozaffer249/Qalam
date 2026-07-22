namespace Qalam.Data.DTOs;

public class AcademicTermDto
{
    public int Id { get; set; }
    public int? CurriculumId { get; set; }
    public string? CurriculumNameAr { get; set; }
    public string? CurriculumNameEn { get; set; }
    public int? AcademicProgramId { get; set; }
    public string? AcademicProgramNameAr { get; set; }
    public string? AcademicProgramNameEn { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
