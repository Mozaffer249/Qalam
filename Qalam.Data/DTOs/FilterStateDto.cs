using System.ComponentModel;

namespace Qalam.Data.DTOs;

/// <summary>
/// Current filter state for education content filtering
/// </summary>
public class FilterStateDto
{
    /// <summary>
    /// Education domain ID (1=School, 2=Quran, 3=Language, 4=Skills)
    /// </summary>
    public int? DomainId { get; set; }
    
    /// <summary>
    /// Curriculum ID (for school domain)
    /// </summary>
    public int? CurriculumId { get; set; }
    
    /// <summary>
    /// Education level ID (e.g., Primary, Secondary)
    /// </summary>
    public int? LevelId { get; set; }
    
    /// <summary>
    /// Grade ID within the education level
    /// </summary>
    public int? GradeId { get; set; }
    
    /// <summary>
    /// Academic term/semester ID
    /// </summary>
    public int? TermId { get; set; }
    
    /// <summary>
    /// Subject ID
    /// </summary>
    public int? SubjectId { get; set; }
    
    /// <summary>
    /// Quran content type ID (for Quran domain): Memorization, Recitation, Tajweed
    /// </summary>
    public int? QuranContentTypeId { get; set; }
    
    /// <summary>
    /// Quran level ID (for Quran domain): Beginner, Intermediate, Advanced
    /// </summary>
    public int? QuranLevelId { get; set; }
    
    /// <summary>
    /// Unit type code for filtering content units.
    /// Values: "QuranSurah" (114 Surahs), "QuranPart" (30 Juz), "SchoolUnit", "LanguageModule"
    /// </summary>
    /// <example>QuranPart</example>
    [DefaultValue("QuranPart")]
    public string? UnitTypeCode { get; set; }
}
