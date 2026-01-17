using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Common;

/// <summary>
/// أيام الأسبوع
/// </summary>
public class DayOfWeekMaster : AuditableEntity
{
    /// <summary>
    /// معرف اليوم (1-7: الأحد للسبت)
    /// </summary>
    public int Id { get; set; }
    
    [Required, MaxLength(30)]
    public string NameAr { get; set; } = null!;
    
    [Required, MaxLength(30)]
    public string NameEn { get; set; } = null!;
    
    /// <summary>
    /// ترتيب العرض
    /// </summary>
    public int OrderIndex { get; set; }
    
    public bool IsActive { get; set; } = true;
}
