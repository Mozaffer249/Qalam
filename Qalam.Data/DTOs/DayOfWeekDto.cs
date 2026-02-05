namespace Qalam.Data.DTOs;

/// <summary>
/// DTO لأيام الأسبوع
/// </summary>
public class DayOfWeekDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public int OrderIndex { get; set; }
}
