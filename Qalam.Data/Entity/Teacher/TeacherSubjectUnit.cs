using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Quran;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// الوحدات المحددة التي يدرسها المعلم (إذا لم يكن يدرس المادة كاملة)
/// للقرآن: يمكن تحديد نوع المحتوى (حفظ/تلاوة/تجويد) والمستوى (نوراني/مبتدئ/متوسط/متقدم)
/// null = يستطيع تدريس كل الأنواع/المستويات
/// </summary>
public class TeacherSubjectUnit : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherSubjectId { get; set; }
    public int UnitId { get; set; }
    
    /// <summary>
    /// نوع المحتوى للقرآن (حفظ/تلاوة/تجويد)
    /// null = يستطيع تدريس كل الأنواع
    /// </summary>
    public int? QuranContentTypeId { get; set; }
    
    /// <summary>
    /// مستوى القرآن (نوراني/مبتدئ/متوسط/متقدم)
    /// null = يستطيع تدريس كل المستويات
    /// </summary>
    public int? QuranLevelId { get; set; }
    
    // Navigation Properties
    public TeacherSubject TeacherSubject { get; set; } = null!;
    public ContentUnit Unit { get; set; } = null!;
    public QuranContentType? QuranContentType { get; set; }
    public QuranLevel? QuranLevel { get; set; }
}
