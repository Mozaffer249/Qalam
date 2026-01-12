using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.Entity.Teaching;

/// <summary>
/// Represents the teaching mode (location/venue) for a teaching session.
/// Defines WHERE the session will take place.
/// طريقة التدريس - تحدد المكان (أين ستتم الجلسة)
/// </summary>
/// <remarks>
/// Available modes:
/// - In-Person (حضوري): Session takes place at a physical location (center, home, school, mosque, etc.)
/// - Online (أونلاين): Session takes place via the internet (Zoom, Teams, Google Meet, etc.)
/// 
/// Note: There is NO "Hybrid" option - each session is either in-person OR online, not both.
/// 
/// This entity is INDEPENDENT from SessionType (which defines the number of participants).
/// They can be combined in 4 ways:
/// 1. In-Person + Individual (حضوري + فردي): Private lesson at a location
/// 2. In-Person + Group (حضوري + جماعي): Lecture in a hall
/// 3. Online + Individual (أونلاين + فردي): One-on-one Zoom session
/// 4. Online + Group (أونلاين + جماعي): Online webinar
/// </remarks>
public class TeachingMode
{
    /// <summary>
    /// Unique identifier for the teaching mode
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique code for the teaching mode
    /// Possible values: "in_person", "online"
    /// </summary>
    [Required, MaxLength(30)]
    public string Code { get; set; } = default!; // in_person, online

    /// <summary>
    /// Arabic name of the teaching mode
    /// Examples: "حضوري", "أونلاين"
    /// </summary>
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;

    /// <summary>
    /// English name of the teaching mode
    /// Examples: "In-Person", "Online"
    /// </summary>
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;

    /// <summary>
    /// Arabic description of the teaching mode
    /// </summary>
    [MaxLength(200)]
    public string? DescriptionAr { get; set; }

    /// <summary>
    /// English description of the teaching mode
    /// </summary>
    [MaxLength(200)]
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// Navigation property to domain teaching modes (which domains support this mode)
    /// </summary>
    public ICollection<DomainTeachingMode> DomainTeachingModes { get; set; } = new List<DomainTeachingMode>();
}

