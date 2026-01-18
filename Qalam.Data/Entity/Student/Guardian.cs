using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Student;

/// <summary>
/// Guardian entity related to the user 
/// </summary>
public class Guardian : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// User ID related to the guardian (optional - taken from User if it has an account)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Full name (optional - taken from User if it has an account)
    /// </summary>
    [MaxLength(200)]
    public string? FullName { get; set; }

    /// <summary>
    /// Phone number (optional - taken from User if it has an account)
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Email (optional - taken from User if it has an account)
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public User? User { get; set; }

    /// <summary>
    /// Guardian can be responsible for multiple students (children or relatives)
    /// </summary>
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
