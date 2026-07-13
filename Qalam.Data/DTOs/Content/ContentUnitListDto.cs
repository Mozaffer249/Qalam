namespace Qalam.Data.DTOs.Content;

/// <summary>
/// Flat list shape for GET /Content/Units — avoids EF navigation cycles in JSON.
/// </summary>
public class ContentUnitListDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? TermId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
    public string UnitTypeCode { get; set; } = default!;
    public int? QuranSurahId { get; set; }
    public int? QuranPartId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
