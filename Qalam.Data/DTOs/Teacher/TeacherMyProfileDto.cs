using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherMyProfileDto
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Nationality { get; set; }
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public TeacherLocation? Location { get; set; }
    public TeacherStatus Status { get; set; }
    public decimal RatingAverage { get; set; }
    public DateTime CreatedAt { get; set; }
}
