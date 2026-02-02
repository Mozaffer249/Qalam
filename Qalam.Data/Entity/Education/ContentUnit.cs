using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class ContentUnit : AuditableEntity
{
    public int Id { get; set; }

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;

    // Academic term this unit belongs to (optional - for curriculum planning)
    public int? TermId { get; set; }
    public AcademicTerm? Term { get; set; }

    [Required, MaxLength(200)]
    public string NameAr { get; set; } = default!;

    [Required, MaxLength(200)]
    public string NameEn { get; set; } = default!;

    public int OrderIndex { get; set; }

    [Required, MaxLength(50)]
    public string UnitTypeCode { get; set; } = "Unit"; // SchoolUnit, LanguageModule, QuranSurah, QuranPart

    // Quran optional links
    public int? QuranSurahId { get; set; }
    public Quran.QuranSurah? QuranSurah { get; set; }

    public int? QuranPartId { get; set; }
    public Quran.QuranPart? QuranPart { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

