namespace Qalam.Data.DTOs;

public class FilterOptionsResponseDto
{
    public FilterStateDto CurrentState { get; set; } = default!;
    public EducationRuleDto Rule { get; set; } = default!;
    public string NextStep { get; set; } = default!; // "Curriculum", "Level", "Grade", "Term", "Subject", "QuranPedagogy", "Unit", "Done"
    public List<FilterOptionDto> Options { get; set; } = new List<FilterOptionDto>();
}
