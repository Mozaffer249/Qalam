using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.Entity.Teaching;

/// <summary>
/// Represents the session type (size/number of participants) for a teaching session.
/// Defines HOW MANY students are in the session.
/// نوع الجلسة - يحدد الحجم (كم عدد الطلاب في الجلسة)
/// </summary>
/// <remarks>
/// Available types:
/// - Individual (فردي): One teacher + One student (private lesson/tutoring)
/// - Group (جماعي): One teacher + Multiple students (class/lecture)
/// 
/// This entity is INDEPENDENT from TeachingMode (which defines the location/venue).
/// They can be combined in 4 ways:
/// 1. In-Person + Individual (حضوري + فردي): Private lesson at home/center
/// 2. In-Person + Group (حضوري + جماعي): Lecture in a classroom (e.g., 25 students)
/// 3. Online + Individual (أونلاين + فردي): One-on-one Zoom session
/// 4. Online + Group (أونلاين + جماعي): Online webinar (e.g., 100 students)
/// 
/// Usage in validation:
/// - If SessionType is "individual", MaxParticipants is optional (or should be 1)
/// - If SessionType is "group", MaxParticipants is REQUIRED and must be > 1
/// </remarks>
public class SessionType
{
    /// <summary>
    /// Unique identifier for the session type
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique code for the session type
    /// Possible values: "individual", "group"
    /// </summary>
    [Required, MaxLength(30)]
    public string Code { get; set; } = default!; // individual, group

    /// <summary>
    /// Arabic name of the session type
    /// Examples: "فردي", "جماعي"
    /// </summary>
    [Required, MaxLength(50)]
    public string NameAr { get; set; } = default!;

    /// <summary>
    /// English name of the session type
    /// Examples: "Individual", "Group"
    /// </summary>
    [Required, MaxLength(50)]
    public string NameEn { get; set; } = default!;

    /// <summary>
    /// Arabic description of the session type
    /// </summary>
    [MaxLength(200)]
    public string? DescriptionAr { get; set; }

    /// <summary>
    /// English description of the session type
    /// </summary>
    [MaxLength(200)]
    public string? DescriptionEn { get; set; }
}

