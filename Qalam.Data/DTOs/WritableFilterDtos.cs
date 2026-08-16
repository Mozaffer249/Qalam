namespace Qalam.Data.DTOs;

public class WritableFilterValueDto
{
    public int Id { get; set; }
    public int SlotId { get; set; }
    public string SlotCode { get; set; } = default!;
    public string? Code { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public bool IsSeeded { get; set; }
}
