namespace Qalam.Data.DTOs.Teacher;

public class TeacherAccountDto
{

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string Token { get; set; } = null!;
}
