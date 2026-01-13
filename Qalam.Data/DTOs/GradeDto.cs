namespace Qalam.Data.DTOs;

public class GradeDto
{
    public int Id { get; set; }
    public int LevelId { get; set; }
    public string LevelNameAr { get; set; } = default!;
    public string LevelNameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
