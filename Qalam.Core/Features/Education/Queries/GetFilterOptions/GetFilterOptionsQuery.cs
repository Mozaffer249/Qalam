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
    /// Grade ID (wizard step 4 — next step is Subject).
    /// </summary>
    public int? GradeId { get; set; }

    /// <summary>University institution ID (university domain).</summary>
    public int? UniversityId { get; set; }

    /// <summary>College / faculty ID.</summary>
    public int? CollegeId { get; set; }

    /// <summary>Department ID.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Academic program ID.</summary>
    public int? AcademicProgramId { get; set; }

    /// <summary>
    /// Subject ID (wizard step 5 — send after gradeId, before termIds).
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// Academic term IDs (wizard step 6 — send after subjectId; repeat param for multi-select).
    /// </summary>
    public List<int>? TermIds { get; set; }

    /// <summary>Skip optional term step when AcademicTermOptional is true.</summary>
    public bool SkipTerm { get; set; }

    /// <summary>
    /// Content unit ID (wizard step 7 — send after picking from unit[]).
    /// </summary>
    public int? ContentUnitId { get; set; }

    /// <summary>
    /// Lesson IDs (wizard step 8 — optional; repeat param for multi-select).
    /// </summary>
    public List<int>? LessonIds { get; set; }

    /// <summary>
    /// Skip optional lesson picker when true (with contentUnitId set).
    /// </summary>
    public bool SkipLessons { get; set; }

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
