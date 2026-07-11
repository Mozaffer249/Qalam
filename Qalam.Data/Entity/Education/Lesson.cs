using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Education;

public class Lesson : AuditableEntity
{
    public int Id { get; set; }
    
    public int UnitId { get; set; }
    public ContentUnit Unit { get; set; } = default!;
    
    [Required, MaxLength(200)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(200)]
    public string NameEn { get; set; } = default!;
    
    public int OrderIndex { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Quran catalog: lessons scoped per content type + level within a unit
    public int? QuranContentTypeId { get; set; }
    public Quran.QuranContentType? QuranContentType { get; set; }

    public int? QuranLevelId { get; set; }
    public Quran.QuranLevel? QuranLevel { get; set; }
}

