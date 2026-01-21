namespace Qalam.Data.DTOs.Teacher;

public class TeacherAccountResponseDto
{
    public TeacherAccountDto Account { get; set; } = null!;
    public RegistrationStepDto NextStep { get; set; } = null!;
}
