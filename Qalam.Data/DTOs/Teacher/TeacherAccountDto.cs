namespace Qalam.Data.DTOs.Teacher;

public class TeacherAccountDto
{
    public int UserId { get; set; }
    public int TeacherId { get; set; }
    public string Token { get; set; } = null!;
}
