namespace Qalam.Data.DTOs;

public class AcademicTermDto
{
    public int Id { get; set; }
    public int CurriculumId { get; set; }
    public string CurriculumNameAr { get; set; } = default!;
    public string CurriculumNameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
