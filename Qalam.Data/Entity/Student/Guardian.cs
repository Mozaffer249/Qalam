using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Student;

/// <summary>
/// كيان ولي الأمر
/// </summary>
public class Guardian : AuditableEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// معرف المستخدم (اختياري - إذا كان له حساب في النظام)
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// الاسم الكامل (مطلوب إذا لم يكن له حساب)
    /// </summary>
    [Required, MaxLength(200)]
    public string FullName { get; set; } = null!;
    
    /// <summary>
    /// رقم الهاتف
    /// </summary>
    [Required, MaxLength(20)]
    public string Phone { get; set; } = null!;
    
    /// <summary>
    /// البريد الإلكتروني
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public User? User { get; set; }
    
    /// <summary>
    /// الطلاب المرتبطين بولي الأمر (Many-to-Many)
    /// </summary>
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
