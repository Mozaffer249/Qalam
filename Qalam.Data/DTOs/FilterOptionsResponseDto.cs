using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs;

public class FilterOptionsResponseDto
{
    public FilterStateDto CurrentState { get; set; } = default!;
    public EducationRuleDto Rule { get; set; } = default!;
    public string NextStep { get; set; } = default!; // "Curriculum", "Level", "Grade", "Term", "Subject", "QuranContentType", "QuranLevel", "Unit", "Done"
    
    // For non-paginated responses (backward compatibility)
    public List<FilterOptionDto> Options { get; set; } = new List<FilterOptionDto>();
    
    // For unit responses (flat structure with pagination at root level)
    [JsonPropertyName("unit")]
    public List<FilterOptionDto>? Unit { get; set; }
    
    // Pagination properties at root level
    public int? TotalCount { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public int? TotalPages { get; set; }
    
    // For Quran domain enhanced response
    [JsonPropertyName("contentTypes")]
    public List<FilterOptionDto>? QuranContentTypes { get; set; }
    
    [JsonPropertyName("levels")]
    public List<FilterOptionDto>? QuranLevels { get; set; }
    
    // For Quran domain auto-selected subject
    [JsonPropertyName("subject")]
    public FilterOptionDto? SelectedSubject { get; set; }
}
