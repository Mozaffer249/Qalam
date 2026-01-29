namespace Qalam.Data.DTOs;

public class FilterOptionDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Code { get; set; } // for domains/subjects
}
