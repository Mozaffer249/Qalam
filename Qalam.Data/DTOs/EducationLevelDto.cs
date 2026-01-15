namespace Qalam.Data.DTOs;

public class EducationLevelDto
{
    public int Id { get; set; }
   
    public int? CurriculumId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
