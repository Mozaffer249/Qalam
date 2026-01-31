namespace Qalam.Data.DTOs;

public class FilterOptionsResponseDto
{
    public FilterStateDto CurrentState { get; set; } = default!;
    public EducationRuleDto Rule { get; set; } = default!;
    public string NextStep { get; set; } = default!; // "Curriculum", "Level", "Grade", "Term", "Subject", "QuranPedagogy", "UnitType", "Unit", "Done"
    
    // For non-paginated responses (backward compatibility)
    public List<FilterOptionDto> Options { get; set; } = new List<FilterOptionDto>();
    
    // For paginated responses (used when pagination is needed)
    public PaginatedFilterOptionsDto? PaginatedOptions { get; set; }
    
    // For Quran domain enhanced response (when UnitTypeCode is provided)
    public List<FilterOptionDto>? QuranContentTypes { get; set; }
    public List<FilterOptionDto>? QuranLevels { get; set; }
    
    // For Quran domain auto-selected subject
    public FilterOptionDto? SelectedSubject { get; set; }
}
