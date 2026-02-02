using System.ComponentModel;
using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Queries.GetFilterOptions;

/// <summary>
/// Query to get filter options for education content
/// </summary>
public class GetFilterOptionsQuery : IRequest<Response<FilterOptionsResponseDto>>
{
    /// <summary>
    /// Education domain ID (1=School, 2=Quran, 3=Language, 4=Skills)
    /// </summary>
    public int DomainId { get; set; }

    /// <summary>
    /// Curriculum ID (for school domain)
    /// </summary>
    public int? CurriculumId { get; set; }

    /// <summary>
    /// Education level ID
    /// </summary>
    public int? LevelId { get; set; }

    /// <summary>
    /// Grade ID
    /// </summary>
    public int? GradeId { get; set; }

    /// <summary>
    /// Academic term IDs (can select multiple terms)
    /// </summary>
    public List<int>? TermIds { get; set; }

    /// <summary>
    /// Subject ID
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// Quran content type ID: Memorization, Recitation, Tajweed
    /// </summary>
    public int? QuranContentTypeId { get; set; }

    /// <summary>
    /// Quran level ID: Beginner, Intermediate, Advanced
    /// </summary>
    public int? QuranLevelId { get; set; }

    /// <summary>
    /// Unit type code for filtering. Values: "QuranSurah" (114 Surahs), "QuranPart" (30 Juz), "SchoolUnit", "LanguageModule"
    /// </summary>
    /// <example>QuranPart</example>
    [DefaultValue("QuranPart")]
    public string? UnitTypeCode { get; set; }

    /// <summary>
    /// Page number for pagination (default: 1)
    /// </summary>
    [DefaultValue(1)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for pagination (default: 20)
    /// </summary>
    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;
}
