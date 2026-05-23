using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Quran;

namespace Qalam.Data.Entity.OpenSessionRequests;

/// <summary>
/// جلسة واحدة ضمن طلب جلسات (واحدة من TotalSessionsCount).
/// تحمل التاريخ المفضل والفترة الزمنية، بالإضافة إلى الوحدات/الدروس المغطاة.
/// </summary>
public class OpenSessionRequestSession : AuditableEntity
{
    public int Id { get; set; }

    public int SessionRequestId { get; set; }

    /// <summary>
    /// ترتيب الجلسة ضمن الطلب (1-based).
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// التاريخ المفضل لإقامة الجلسة.
    /// </summary>
    public DateOnly? PreferredDate { get; set; }

    /// <summary>
    /// الفترة الزمنية المفضلة (FK إلى TimeSlot).
    /// </summary>
    public int? TimeSlotId { get; set; }

    /// <summary>
    /// مدة الجلسة بالدقائق.
    /// </summary>
    public int DurationMinutes { get; set; } = 60;

    /// <summary>
    /// نوع محتوى القرآن (مطلوب فقط لمجال القرآن).
    /// </summary>
    public int? QuranContentTypeId { get; set; }

    /// <summary>
    /// مستوى القرآن (مطلوب فقط لمجال القرآن).
    /// </summary>
    public int? QuranLevelId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation Properties
    public OpenSessionRequest OpenSessionRequest { get; set; } = null!;
    public TimeSlot? TimeSlot { get; set; }
    public QuranContentType? QuranContentType { get; set; }
    public QuranLevel? QuranLevel { get; set; }

    public ICollection<OpenSessionRequestSessionUnit> Units { get; set; } = new List<OpenSessionRequestSessionUnit>();
}
