using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.DTOs.Admin;

/// <summary>Optional reason for admin hard-delete of a teacher account.</summary>
public class DeleteTeacherRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
