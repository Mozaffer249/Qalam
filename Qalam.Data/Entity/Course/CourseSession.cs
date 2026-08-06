using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Quran;

namespace Qalam.Data.Entity.Course;

public class CourseSession : AuditableEntity
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int SessionNumber { get; set; }
    public int DurationMinutes { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Quran content type for this session (null for non-Quran / all).</summary>
    public int? QuranContentTypeId { get; set; }

    /// <summary>Quran level for this session (null for non-Quran / all).</summary>
    public int? QuranLevelId { get; set; }

    public Course Course { get; set; } = null!;
    public QuranContentType? QuranContentType { get; set; }
    public QuranLevel? QuranLevel { get; set; }
    public ICollection<CourseSessionUnit> Units { get; set; } = new List<CourseSessionUnit>();
    public ICollection<CourseSessionContentLink> ContentLinks { get; set; } = new List<CourseSessionContentLink>();
}
